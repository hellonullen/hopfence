FROM node:20-alpine
WORKDIR /app

COPY packages/hopfence-express/ packages/hopfence-express/
COPY examples/express-nginx/package.json examples/express-nginx/
COPY examples/express-nginx/app.js examples/express-nginx/

WORKDIR /app/examples/express-nginx
RUN npm install --omit=dev

EXPOSE 3000
CMD ["node", "app.js"]
