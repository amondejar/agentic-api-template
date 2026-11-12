// Acts as an AI agent against a running agentic-api: lists tools, calls them, opens the review panel and checks
// that destructive tools never run without the user. Usage: node tests/mcp-agent.mjs http://localhost:5080
const base = process.argv[2] ?? 'http://localhost:5080';
let id = 0, passed = 0;
const ok = (cond, what, detail) => { if (!cond) { console.error(`  ✗ ${what}`, detail ?? ''); process.exit(1); } console.log(`  ✓ ${what}`); passed++; };

async function mcp(method, params = {}, protocol = '2025-11-25') {
  const res = await fetch(`${base}/mcp`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', accept: 'application/json, text/event-stream', 'mcp-protocol-version': protocol },
    body: JSON.stringify({ jsonrpc: '2.0', id: ++id, method, params }),
  });
  const text = await res.text();
  const json = text.trimStart().startsWith('{') ? text : text.split('\n').filter(l => l.startsWith('data:')).map(l => l.slice(5)).join('');
  return { status: res.status, body: json ? JSON.parse(json) : null, raw: text };
}
const api = async (path, init) => { const r = await fetch(`${base}/api${path}`, init); return { status: r.status, body: r.headers.get('content-type')?.includes('json') ? await r.json() : await r.text() }; };

console.log('MCP server');
const init = await mcp('initialize', { protocolVersion: '2025-11-25', capabilities: { extensions: { 'io.modelcontextprotocol/ui': { mimeTypes: ['text/html;profile=mcp-app'] } } }, clientInfo: { name: 'e2e', version: '1' } });
ok(init.status === 200 && init.body?.result?.serverInfo, 'initialize', init.raw);

const list = await mcp('tools/list');
const tools = Object.fromEntries((list.body?.result?.tools ?? []).map(t => [t.name, t]));
for (const name of ['loans_list', 'loans_get', 'loans_approve', 'loans_reject', 'review_loans'])
  ok(tools[name], `tool ${name} is listed`, Object.keys(tools));
ok(tools.loans_reject.annotations?.destructiveHint === true, 'loans_reject is marked destructive');
ok(tools.loans_list.annotations?.readOnlyHint === true, 'loans_list is read-only');
ok(tools.review_loans._meta?.ui?.resourceUri === 'ui://loans/review', 'review_loans opens the review panel');
ok(!tools.mcp_razor_render || tools.mcp_razor_render._meta?.ui?.visibility?.includes('app'), 'panel plumbing tools are hidden from the model');

console.log('Lean results');
ok(tools.loans_list.inputSchema.properties.fields?.items?.enum?.includes('applicant'), 'loans_list offers a fields argument');
ok(!(tools.loans_list.inputSchema.required ?? []).includes('status'), 'optional filters are not required');
const slim = await mcp('tools/call', { name: 'loans_list', arguments: { fields: ['id', 'applicant'] } });
const slimText = slim.body?.result?.content?.[0]?.text ?? '';
ok(slimText.startsWith('[{"id":"L-1042","applicant":"María López"}'), 'the agent gets only the fields it asks for', slimText.slice(0, 200));
ok(!slimText.includes('null'), 'no null fields in the text the model reads', slimText.slice(0, 200));

console.log('Agent reads data');
const listed = await mcp('tools/call', { name: 'loans_list', arguments: { status: 'Pending', minAmount: 20000 } });
const listedText = JSON.stringify(listed.body?.result);
ok(!listed.body?.result?.isError && listedText.includes('L-1043') && !listedText.includes('L-1044'), 'loans_list filters by status and amount', listedText.slice(0, 300));

console.log('Review panel');
const review = await mcp('tools/call', { name: 'review_loans', arguments: {} });
const view = review.body?.result?._meta?.['mcpRazor/view'];
ok(view?.html?.includes('Pending loan applications') && view.html.includes('María López'), 'review_loans renders the panel on the server', JSON.stringify(review.body).slice(0, 300));
const resource = await mcp('resources/read', { uri: 'ui://loans/review' });
ok(resource.body?.result?.contents?.[0]?.mimeType === 'text/html;profile=mcp-app', 'the panel resource is an MCP App');

console.log('A person decides in the panel');
const envelope = r => r.body?.result?.structuredContent ?? JSON.parse(r.body?.result?.content?.[0]?.text ?? '{}');
const click = async (viewId, html, label) => {
  const handler = html.match(new RegExp(`data-mcp-on-click="(\\d+)"[^>]*>\\s*${label}`))?.[1];
  ok(handler, `the panel has a "${label}" button`, html.slice(0, 200));
  const r = await mcp('tools/call', { name: 'mcp_razor_event', arguments: { viewId, handler: Number(handler), type: 'click', payload: {} } });
  ok(!r.body?.result?.isError, `clicking "${label}" runs on the server`, JSON.stringify(r.body).slice(0, 300));
  return envelope(r).html;
};
let html = await click(view.viewId, view.html, 'Review');
ok(html.includes('Reason (required to reject)'), 'the review row opens');
html = await click(view.viewId, html, 'Approve L-1042');
ok(html.includes('status-approved'), 'the panel shows the application approved');
ok((await api('/loans/L-1042')).body.status === 'Approved', 'the decision reached the API: no confirmation needed when a person clicks');

console.log('Destructive tools need the user');
const reject = await mcp('tools/call', { name: 'loans_reject', arguments: { id: 'L-1043', body: { reason: 'Debt ratio above 40 %' } } });
const rejectText = JSON.stringify(reject.body?.result ?? reject.body);
ok(reject.body?.result?.isError === true || reject.body?.error, 'loans_reject does not run on its own', rejectText.slice(0, 400));
ok(/approval|approve|\/approvals\//i.test(rejectText), 'the agent gets an approval link to pass on', rejectText.slice(0, 400));
ok((await api('/loans/L-1043')).body.status === 'Pending', 'the application is still Pending');

const link = rejectText.match(/https?:\/\/[^\s"\\]+\/approvals\/[A-Za-z0-9_-]+/)?.[0];
ok(link, 'the approval link points at this API', rejectText.slice(0, 400));
const page = await fetch(link);
ok(page.status === 200 && (await page.text()).includes('L-1043'), 'the approval page shows what the agent asks');

console.log('REST API');
ok((await api('/loans/L-1045/reject', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ reason: '' }) })).status === 400, 'a rejection without a reason is a validation error');
const approved = await api('/loans/L-1044/approve', { method: 'POST' });
ok(approved.status === 200 && approved.body.status === 'Approved', 'a person approves through the API');
ok((await api('/loans/L-1044/approve', { method: 'POST' })).status === 409, 'approving twice is a conflict');

console.log(`\nAll ${passed} agent checks passed.`);
