'use strict';

const crypto = require('crypto');

/**
 * HopFence diagnostic probe middleware for Express.js
 *
 * @param {Object} [options]
 * @param {boolean} [options.enabled=false] - Whether the diagnostic probe is enabled
 * @param {string} [options.token] - Bearer token required to access the probe
 * @param {string} [options.path='/_hopfence/probe'] - Path to mount the probe endpoint
 * @param {string[]} [options.allowedIps=[]] - Optional IP allowlist for probe queries
 * @param {number} [options.rateLimit=60] - Max requests per minute per IP
 * @returns {import('express').RequestHandler}
 */
function hopFence(options = {}) {
  const probePath = options.path || '/_hopfence/probe';
  const allowedIps = new Set(options.allowedIps || []);
  const rateLimit = options.rateLimit || 60;
  const rateLimits = new Map();

  // Helper for timing-safe comparison
  function timingSafeCompare(a, b) {
    if (!a || !b) return false;
    const bufA = Buffer.from(a, 'utf8');
    const bufB = Buffer.from(b, 'utf8');
    if (bufA.length !== bufB.length) {
      return false;
    }
    return crypto.timingSafeEqual(bufA, bufB);
  }

  // Rate limiting helper
  function checkRateLimit(ip) {
    if (!ip || rateLimit <= 0) return true;
    const now = Date.now();
    const entry = rateLimits.get(ip) || { count: 0, reset: now + 60000 };
    if (now > entry.reset) {
      entry.count = 1;
      entry.reset = now + 60000;
    } else {
      entry.count += 1;
    }
    rateLimits.set(ip, entry);
    return entry.count <= rateLimit;
  }

  return function hopFenceMiddleware(req, res, next) {
    const isProbePath = req.path === probePath;
    if (!isProbePath) {
      return next();
    }

    // Only respond to GET
    if (req.method !== 'GET') {
      return res.status(405).set('Allow', 'GET').send('Method Not Allowed');
    }

    // 1. Check enabled state
    const isEnabled = options.enabled === true ||
      process.env.HOPFENCE_ENABLED === 'true' ||
      process.env.HOPFENCE_ENABLED === '1';

    if (!isEnabled) {
      return res.status(404).send('Not Found');
    }

    // 2. Anti-cache headers
    res.set({
      'Cache-Control': 'no-store, no-cache, must-revalidate',
      'Pragma': 'no-cache'
    });

    const socketPeerIp = req.socket ? req.socket.remoteAddress : undefined;

    // 3. Optional IP allowlist
    if (allowedIps.size > 0 && socketPeerIp) {
      if (!allowedIps.has(socketPeerIp)) {
        return res.status(403).send('Forbidden: IP not authorized for HopFence probe');
      }
    }

    // 4. Rate limiting
    if (socketPeerIp && !checkRateLimit(socketPeerIp)) {
      res.set('Retry-After', '60');
      return res.status(429).send('Too Many Requests');
    }

    // 5. Authenticate Bearer Token
    const expectedToken = options.token || process.env.HOPFENCE_TOKEN;
    if (!expectedToken) {
      res.set('WWW-Authenticate', 'Bearer error="token_not_configured"');
      return res.status(401).send('Unauthorized: HOPFENCE_TOKEN is not configured');
    }

    const authHeader = req.headers.authorization || '';
    if (!authHeader.toLowerCase().startsWith('bearer ')) {
      res.set('WWW-Authenticate', 'Bearer');
      return res.status(401).send('Unauthorized: Missing Bearer token');
    }

    const providedToken = authHeader.slice(7).trim();
    if (!timingSafeCompare(providedToken, expectedToken)) {
      res.set('WWW-Authenticate', 'Bearer error="invalid_token"');
      return res.status(401).send('Unauthorized: Invalid Bearer token');
    }

    // 6. Gather probe data strictly without leaking body, cookies, query, or token
    const headers = req.headers || {};
    const xForwardedFor = headers['x-forwarded-for'];
    const xForwardedProto = headers['x-forwarded-proto'];
    const xForwardedHost = headers['x-forwarded-host'];

    const probeResponse = {
      schemaVersion: 1,
      framework: 'express',
      request: {
        clientIp: req.ip || socketPeerIp || null,
        socketPeerIp: socketPeerIp || null,
        scheme: req.protocol || (req.secure ? 'https' : 'http'),
        host: req.hostname || headers.host || null,
        pathBase: req.baseUrl || '',
        forwardedHeaders: {
          xForwardedForPresent: Boolean(xForwardedFor),
          xForwardedProtoPresent: Boolean(xForwardedProto),
          xForwardedHostPresent: Boolean(xForwardedHost),
          rawXForwardedFor: Array.isArray(xForwardedFor) ? xForwardedFor.join(', ') : (xForwardedFor || null),
          rawXForwardedProto: Array.isArray(xForwardedProto) ? xForwardedProto[0] : (xForwardedProto || null),
          rawXForwardedHost: Array.isArray(xForwardedHost) ? xForwardedHost[0] : (xForwardedHost || null)
        }
      }
    };

    res.set('Content-Type', 'application/json; charset=utf-8');
    return res.status(200).json(probeResponse);
  };
}

module.exports = hopFence;
module.exports.hopFence = hopFence;
module.exports.default = hopFence;
