# The Wordrise website (Angular with server-side rendering). Build from the web/ folder:
#   docker build -t wordrise-web web

FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# The server bundle already contains what it needs (Express and Angular), so the final image
# holds only the build output.
FROM node:24-alpine AS runtime
WORKDIR /app

COPY --from=build /app/dist/wordrise-web ./

ENV NODE_ENV=production \
    PORT=4000

# Angular refuses requests whose Host header it does not know (protection against forged hosts):
# set NG_ALLOWED_HOSTS to the site's domain, comma separated.
USER node
EXPOSE 4000

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD node -e "fetch('http://localhost:4000/').then(r => process.exit(r.ok ? 0 : 1)).catch(() => process.exit(1))"

CMD ["node", "server/server.mjs"]
