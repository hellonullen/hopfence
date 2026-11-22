'use strict';

const express = require('express');
const hopFence = require('@hopfence/express');

const app = express();
const port = process.env.PORT || 3000;

// Set trusted proxy (e.g. 1 hop behind Docker or loopback)
app.set('trust proxy', 1);

// Mount HopFence diagnostic probe
app.use(hopFence({
  enabled: process.env.HOPFENCE_ENABLED === 'true',
  token: process.env.HOPFENCE_TOKEN || 'demo-express-secret-token'
}));

app.get('/', (req, res) => {
  res.json({
    message: 'HopFence Express Sample App behind Nginx',
    clientIp: req.ip,
    protocol: req.protocol,
    hostname: req.hostname
  });
});

app.listen(port, () => {
  console.log(`Express sample app listening on port ${port}`);
});
