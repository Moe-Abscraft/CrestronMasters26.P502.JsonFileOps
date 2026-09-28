# cmpg-521-ui — Angular CH5 touchpanel UI

Angular 18 CH5 project for the Crestron Masters JSON Objects & File Operations kit.
See the [repo README](../../README.md) for how this fits with the C# side.

## Prerequisites

- Node 18+ and npm
- Crestron CH5 CLI tooling comes in via `npm install` (`@crestron/ch5-utilities-cli`,
  `@crestron/ch5-shell-utilities-cli`)

## Develop

```bash
npm install
npm start          # ng serve --open on http://localhost:4200
npm run watch      # rebuild on change, development configuration
npm test           # Karma + Jasmine
```

In the browser the CH5 bridge is not live, so signal feedback is inert — use it for
layout and component work.

## Build and deploy

```bash
npm run compile    # ng build, then ch5-cli archive -> archive/cmpg-521-ui.ch5z
npm run deployt    # deploy to touchpanel   (192.168.2.31)
npm run deployp    # deploy to controlsystem (192.168.2.95)
```

The two IPs are hard-coded in `package.json`. Change them to match your rack, or run
`ch5-cli deploy` directly with `-H <host>`.

`npm run cresprep` rewrites `type="module"` to `defer` in the built `index.html` — some
panel firmware needs that. It is not part of `compile`; run it between `build` and
`build:archive` if you hit a blank panel.

## Contract

- `contract.cce` — Contract Editor source
- `output/contract/` — generated interface + programming output
- `src/config/AppContract.cse2j` — **the authored contract**, under version control
- `src/config/contract.cse2j` — generated copy, gitignored (see below)

### Why there are two files

WebXPanel fetches the contract from a hard-coded path, `<base>config/contract.cse2j`
— there is no setting for it, so `ng serve` and `ng build` only work if the file is
published under exactly that name.

`ch5-cli archive -c <file>` is different: it accepts any name (the extension must be
`.cse2j`) and copies the contents into the package as `config/contract.cse2j` itself.

`tools/sync-contract.js` bridges the two. It finds the single authored `*.cse2j` in
`src/config`, whatever it is called, and publishes it as `contract.cse2j`. It runs
automatically via the `prestart`, `prebuild` and `prewatch` hooks, or on demand:

```bash
npm run sync:contract
```

`angular.json` ships only `contract.cse2j`, so the authored file does not end up
duplicated in the bundle.

After changing the contract in Contract Editor, export it over
`src/config/AppContract.cse2j` and commit that. The copy regenerates on the next
`npm start` or `npm run build`. Keep exactly one authored `.cse2j` in `src/config` —
the script fails loudly if it finds none or several.

## Structure

```
src/app/
├─ start-page/       landing page
├─ header/           header bar
├─ gatherinfo/       async gather demo
├─ csharp-wrapper/   signal bridge demo (page nav, serial send)
├─ logger/           on-panel log view
├─ service/          controller.service.ts — all CrComLib signal state
└─ helpers/          CrComLibHelpers.ts — subscribe/publish wrappers
```

All CH5 signal traffic goes through `ControllerService`; components subscribe to its
`Subject`s and never touch `CrComLib` directly.
