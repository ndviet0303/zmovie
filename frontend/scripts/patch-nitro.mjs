import { readFile, writeFile } from 'node:fs/promises'

const file = new URL('../node_modules/nitropack/dist/rollup/index.mjs', import.meta.url)
const source = await readFile(file, 'utf8')
const patched = source
  .replaceAll("from '${handler}';", 'from ${JSON.stringify(handler)};')
  .replaceAll("import('${handler}')", 'import(${JSON.stringify(handler)})')
  .replaceAll("from '${plugin}';", 'from ${JSON.stringify(plugin)};')

if (patched !== source) {
  await writeFile(file, patched)
  console.log('Patched Nitro handler imports for paths containing apostrophes.')
}
