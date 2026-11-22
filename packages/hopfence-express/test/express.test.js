const test = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const hopFence = require('../index.js');

test('HopFence Express Middleware Tests', async (t) => {
  const token = 'test-secret-token-12345';

  function createServer(middleware) {
    return http.createServer((req, res) => {
      // Minimal Express-like mock
      req.path = req.url.split('?')[0];
      req.ip = '10.0.0.1';
      req.protocol = 'http';
      req.hostname = 'test.local';
      res.status = function (code) {
        this.statusCode = code;
        return this;
      };
      res.set = function (headers) {
        for (const [k, v] of Object.entries(headers)) {
          this.setHeader(k, v);
        }
        return this;
      };
      res.json = function (obj) {
        this.setHeader('Content-Type', 'application/json');
        this.end(JSON.stringify(obj));
      };
      res.send = function (data) {
        this.end(data);
      };

      middleware(req, res, () => {
        res.statusCode = 200;
        res.end('next called');
      });
    });
  }

  await t.test('returns 404 when probe is disabled', async () => {
    const server = createServer(hopFence({ enabled: false, token }));
    await new Promise((resolve) => server.listen(0, resolve));
    const port = server.address().port;

    const res = await fetch(`http://127.0.0.1:${port}/_hopfence/probe`);
    assert.equal(res.status, 404);

    server.close();
  });

  await t.test('returns 401 when token is missing', async () => {
    const server = createServer(hopFence({ enabled: true, token }));
    await new Promise((resolve) => server.listen(0, resolve));
    const port = server.address().port;

    const res = await fetch(`http://127.0.0.1:${port}/_hopfence/probe`);
    assert.equal(res.status, 401);

    server.close();
  });

  await t.test('returns 401 when token is invalid', async () => {
    const server = createServer(hopFence({ enabled: true, token }));
    await new Promise((resolve) => server.listen(0, resolve));
    const port = server.address().port;

    const res = await fetch(`http://127.0.0.1:${port}/_hopfence/probe`, {
      headers: { Authorization: 'Bearer wrong-token' }
    });
    assert.equal(res.status, 401);

    server.close();
  });

  await t.test('returns 200 with schema v1 when authenticated', async () => {
    const server = createServer(hopFence({ enabled: true, token }));
    await new Promise((resolve) => server.listen(0, resolve));
    const port = server.address().port;

    const res = await fetch(`http://127.0.0.1:${port}/_hopfence/probe`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Forwarded-For': '203.0.113.10',
        'X-Forwarded-Proto': 'https'
      }
    });

    assert.equal(res.status, 200);
    const body = await res.json();
    assert.equal(body.schemaVersion, 1);
    assert.equal(body.framework, 'express');
    assert.equal(body.request.clientIp, '10.0.0.1');
    assert.equal(body.request.forwardedHeaders.xForwardedForPresent, true);
    assert.equal(body.request.forwardedHeaders.xForwardedProtoPresent, true);

    server.close();
  });
});
