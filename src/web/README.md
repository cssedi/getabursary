# GetABursary web (Angular SSR)

Angular (standalone, signals) with SSR, Tailwind CSS and Angular CDK. See `specs/05-web.md`.

## Run
```bash
cd src/web
npm ci
npm start            # http://localhost:4200, /api is proxied to http://localhost:5266
```
Run the API on port 5266 (`dotnet run --project src/GetABursary.Api`) for data.

## Test, lint, build
```bash
npm test -- --watch=false
npm run lint
npm run build        # production build, enforces size budgets
npm run serve:ssr    # serves the SSR build on :4000 (PORT to override; API_INTERNAL_URL for the API base)
```
