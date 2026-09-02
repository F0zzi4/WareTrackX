import { mkdir } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from 'playwright'

const appUrl = process.env.APP_URL ?? 'http://127.0.0.1:5174'
const rootDir = dirname(fileURLToPath(new URL('../package.json', import.meta.url)))
const screenshotDir = join(rootDir, 'artifacts', 'screenshots')

await mkdir(screenshotDir, { recursive: true })

const browser = await chromium.launch()

for (const target of [
  { name: 'desktop', width: 1440, height: 1000, isMobile: false },
  { name: 'mobile', width: 390, height: 844, isMobile: true },
]) {
  const page = await browser.newPage({
    viewport: { width: target.width, height: target.height },
    isMobile: target.isMobile,
  })

  await page.goto(appUrl, { waitUntil: 'networkidle' })
  await page.getByRole('button', { name: 'Sign in' }).click()
  await page.getByRole('heading', { name: 'Dashboard' }).waitFor()

  const chartCount = await page.locator('.recharts-responsive-container').count()
  if (chartCount < 3) {
    throw new Error(`Expected at least 3 charts, found ${chartCount}`)
  }

  const sectorCount = await page.locator('.recharts-pie-sector path').count()
  if (sectorCount < 4) {
    throw new Error(`Expected 4 risk sectors, found ${sectorCount}`)
  }

  const layoutWidth = await page.evaluate(() => document.documentElement.scrollWidth)
  if (layoutWidth > target.width + 2) {
    throw new Error(`Unexpected page overflow: ${layoutWidth}px on ${target.name}`)
  }

  await page.mouse.move(4, 4)
  await page.screenshot({
    fullPage: true,
    path: join(screenshotDir, `${target.name}-dashboard.png`),
  })

  await page.getByRole('button', { name: 'Toggle theme' }).click()
  await page.mouse.move(4, 4)
  await page.screenshot({
    fullPage: true,
    path: join(screenshotDir, `${target.name}-dark.png`),
  })

  await page.close()
}

await browser.close()
