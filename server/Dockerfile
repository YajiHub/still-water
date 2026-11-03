# Still Water game server (Colyseus). Built and run by the Lab 6 pipeline on the VM; the same image can go to Render.

# --- build: install everything (TypeScript is a dev dependency) and compile to build/ ---
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json .npmrc ./
RUN npm ci
COPY tsconfig.json tsconfig.build.json ./
COPY src ./src
RUN npm run build

# --- runtime: production dependencies + compiled JS only ---
FROM node:24-alpine
WORKDIR /app
ENV NODE_ENV=production
COPY package.json package-lock.json .npmrc ./
RUN npm ci --omit=dev && npm cache clean --force
COPY --from=build /app/build ./build
COPY .env.production ./

# The commit SHA in CI/CD deploys (shown by / and /health); "dev" if built by hand.
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION

USER node
EXPOSE 2567
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:2567/health || exit 1
CMD ["node", "build/index.js"]
