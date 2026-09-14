/**
 * sync-contract.js
 *
 * WebXPanel fetches the contract from a HARD-CODED path: `<base>config/contract.cse2j`.
 * (See @crestron/ch5-webxpanel: `t = e + "config/contract.cse2j", fetch(t)`.)
 * There is no setting for it.
 *
 * `ch5-cli archive -c <file>` does not care about the name -- it copies whatever
 * you pass into the package as config/contract.cse2j itself -- but `ng serve`
 * and `ng build` do no such renaming.
 *
 * So: author the contract under whatever name Contract Editor produces
 * (e.g. AppContract.cse2j) and let this script publish a copy under the one
 * name the runtime will look for. The copy is generated and gitignored.
 *
 * Runs automatically via the prestart / prebuild / prewatch npm hooks.
 */

const fs = require('fs');
const path = require('path');

const CONFIG_DIR = path.join(__dirname, '..', 'src', 'config');
const CANONICAL = 'contract.cse2j';
const target = path.join(CONFIG_DIR, CANONICAL);

function fail(message) {
  console.error('\n[sync-contract] ' + message + '\n');
  process.exit(1);
}

if (!fs.existsSync(CONFIG_DIR)) {
  fail('No config directory at ' + CONFIG_DIR);
}

// The authored contract is any .cse2j in src/config that is not the generated copy.
const candidates = fs
  .readdirSync(CONFIG_DIR)
  .filter((f) => f.toLowerCase().endsWith('.cse2j') && f !== CANONICAL);

if (candidates.length === 0) {
  fail(
    'No authored *.cse2j found in src/config.\n' +
      '  Export the contract from Contract Editor into that folder.\n' +
      '  Any file name works -- this script publishes it as ' + CANONICAL + '.'
  );
}

if (candidates.length > 1) {
  fail(
    'Expected one authored *.cse2j in src/config, found ' + candidates.length + ':\n' +
      candidates.map((c) => '    ' + c).join('\n') +
      '\n  Keep exactly one so there is no ambiguity about which contract ships.'
  );
}

const source = path.join(CONFIG_DIR, candidates[0]);
const content = fs.readFileSync(source);

if (fs.existsSync(target) && fs.readFileSync(target).equals(content)) {
  console.log('[sync-contract] ' + candidates[0] + ' -> ' + CANONICAL + ' (already up to date)');
  process.exit(0);
}

fs.writeFileSync(target, content);
console.log('[sync-contract] ' + candidates[0] + ' -> ' + CANONICAL);
