# Build context is the repo root: docker build -f src/web/Dockerfile .
FROM node:22-alpine AS build
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

FROM node:22-alpine
WORKDIR /web
ENV NODE_ENV=production \
    PORT=4000
COPY --from=build /web/package.json /web/package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force
COPY --from=build /web/dist ./dist
EXPOSE 4000
USER node
CMD ["node", "dist/getabursary-web/server/server.mjs"]
