// Requirements
const fs = require('fs')
const { execSync } = require('child_process')

const cypress = require('cypress')

// Get command line arguments
const ARGV = process.argv.slice(2)

// Start vtex login proccess before?
const NO_VTEX = ARGV.includes('--no-vtex')

// Save start date
const START = Date.now()

function readVtexFile() {
  return fs.readFileSync('.vtex.json')
}

const WORKSPACE = () => {
  // Return dev
  if (ARGV.includes('--dev')) {
    return 'dev'
  }

  // If file empty, return dev... else last workspace used
  if (NO_VTEX) {
    const VTEX_JSON = readVtexFile()
    const VTEX_WORKSPACE = JSON.parse(VTEX_JSON).WORKSPACE

    return typeof VTEX_WORKSPACE === 'undefined' ? 'dev' : VTEX_WORKSPACE
  }

  // Create a new workspace based on start date
  return `e2e${START.toString().substr(-7)}`
}

// Cypress binary
const CYPRESS_BIN = './node_modules/.bin/cypress'

// Run in headed mode
const HEADED = ARGV.includes('--show') ? '--headed' : ' '

// Hold errors and try hard try again control
const testErrors = []
let tryAgain = false

// Tests to RUN
const CY_SETUP =
  typeof process.env.CY_SETUP === 'undefined'
    ? '**/workspace/setup*'
    : process.env.CY_SETUP

const CY_TESTS =
  typeof process.env.CY_TESTS === 'undefined'
    ? ['payment.spec.js']
    : [process.env.CY_TESTS]

const CY_TDOWN =
  typeof process.env.CY_TDOWN === 'undefined'
    ? '**/workspace/teardown*'
    : process.env.CY_TDOWN

// Scripts
const VTEX_SH =
  typeof process.env.VTEX_SH === 'undefined'
    ? 'cypress/plugins/vtex.sh'
    : process.env.VTEX_SH

const OTP_SH =
  typeof process.env.OTP_SH === 'undefined'
    ? 'cypress/plugins/otp.sh'
    : process.env.OTP_SH

// Toolbelt
const VTEX_BIN =
  typeof process.env.VTEX_BIN === 'undefined'
    ? '$HOME/.cache/vtex-e2e'
    : process.env.VTEX_BIN

// Account
const VTEX_ACCOUNT =
  typeof process.env.VTEX_ACCOUNT === 'undefined'
    ? 'sandboxusdev'
    : process.env.VTEX_ACCOUNT

// Load secrets
let vtexJson = {}

if (typeof process.env.VTEX_CYPRESS === 'undefined') {
  process.stderr.write(
    '[QE] ===> You must configure the VTEX_CYPRESS on your secrets before run the test.\n'
  )
  process.exit(126)
} else {
  try {
    JSON.parse(process.env.VTEX_CYPRESS)
  } catch (error) {
    process.stderr.write(
      '[QE] ===> You must reconfigure the VTEX_CYPRESS, the JSON is invalid.\n'
    )
    process.exit(125)
  }

  // Save secrets parsed and expose it to Cypress
  const { VTEX_CYPRESS } = process.env

  vtexJson = JSON.parse(VTEX_CYPRESS)
  execSync(`echo ${JSON.stringify(VTEX_CYPRESS)} > cypress.env.json`)

  // Set env variables to be used by bash scripts on plugins folder
  process.env.VTEX_ACCOUNT = vtexJson.VTEX_ACCOUNT
  process.env.TWILIO_USER = vtexJson.TWILIO_USER
  process.env.TWILIO_TOKEN = vtexJson.TWILIO_TOKEN
}

// Open Cypress
function openCypress() {
  const CYPRESS_CONFIG = {
    env: {
      WORKSPACE: WORKSPACE(),
      VTEX_BIN,
      OTP_SH,
      IN_CYPRESS: true,
      VTEX_ACCOUNT,
    },
    config: {
      baseUrl: `https://${WORKSPACE()}--sandboxusdev.myvtex.com`,
      testFiles: '**/*.*',
    },
  }

  cypress.open(CYPRESS_CONFIG)
}

// Run Cypress
async function runCypress(testFiles) {
  // If undefined, throw error
  if (typeof testFiles === 'undefined') {
    throw new Error('You must pass the testFiles regex.')
  }

  // Constants to pass to run command
  const BASE_URL = `https://${WORKSPACE()}--sandboxusdev.myvtex.com`
  const GROUP_NAME = `${WORKSPACE()}/${testFiles}`
  const TEST_NAME = `**/avalara/${testFiles}*`
  const KEY = vtexJson.CYPRESS_DASHBOARD_KEY
  const DASHBOARD = vtexJson.CYPRESS_DASHBOARD
    ? '--record ' + `--key ${KEY} ` + `--group ${GROUP_NAME}`
    : ''

  const CMD =
    `${CYPRESS_BIN} run ${HEADED} -b chrome -P . ` +
    `-e WORKSPACE=${WORKSPACE()},VTEX_BIN=${VTEX_BIN},OTP_SH=${OTP_SH},VTEX_ACCOUNT=${VTEX_ACCOUNT},IN_CYPRESS=true ` +
    `-c baseUrl="${BASE_URL}",testFiles="${TEST_NAME}" `

  // Try to run the test
  try {
    const TRY1 = vtexJson.CYPRESS_DASHBOARD ? '-try-1' : ''

    execSync(`${CMD} ${DASHBOARD}${TRY1} 2>/dev/null`, {
      stdio: 'inherit',
    })
    tryAgain = false
  } catch (error) {
    if (tryAgain) {
      tryAgain = false
      process.stdout.write(
        `[QE] ===> Error on "${TEST_NAME}", but trying it again...\n`
      )
      try {
        const TRY2 = vtexJson.CYPRESS_DASHBOARD ? '-try-2' : ''

        execSync(`${CMD} ${DASHBOARD}${TRY2} 2>/dev/null`, { stdio: 'inherit' })
      } catch (anotherError) {
        testErrors.push(testFiles)
        process.stdout.write(
          `[QE] ===> Error again on "${TEST_NAME}", taking note of it...\n`
        )
      }
    }
  }
}

// Start the script
function vtexBackground() {
  if (NO_VTEX) {
    process.stdout.write(`[QE] ===> Starting without vtex...\n`)
  } else {
    process.stdout.write(`[QE] ===> Calling vtex in background...\n`)
    execSync(VTEX_SH, { stdio: 'inherit' })
  }
}

// Set up the environment
async function vtexSetUp() {
  if (NO_VTEX) {
    process.stdout.write(
      `[QE] ===> No VTEX detected, skipping environment set up...\n`
    )
  } else {
    process.stdout.write(`[QE] ===> Setting up the environment...\n`)
    const BASE_URL = `https://${WORKSPACE()}--sandboxusdev.myvtex.com`
    const CMD =
      `${CYPRESS_BIN} run ${HEADED} -b chrome -P . ` +
      `-e WORKSPACE=${WORKSPACE()},VTEX_BIN=${VTEX_BIN},OTP_SH=${OTP_SH},VTEX_ACCOUNT=${VTEX_ACCOUNT},IN_CYPRESS=true ` +
      `-c baseUrl="${BASE_URL}",testFiles="${CY_SETUP}" `

    try {
      execSync(`${CMD} 2>/dev/null`, { stdio: 'inherit' })
    } catch (error) {
      process.stderr.write(
        `[QE] ===> Error on setting up the environment, calling teardown and finishing the test...\n`
      )
      vtexTearDown()
      process.stderr.write(
        `[QE] ===> Please, try the job again using the 'Re-run all jobs' from GitHub Actions.\n`
      )
      process.exit(128)
    }
  }
}

// Run tests
async function runTests() {
  for (const test of CY_TESTS) {
    tryAgain = true
    let failedDepdency = null

    // Check test dependency
    switch (test) {
      case '2.6':
        // Needs 2.1, 2.2 and 2.5 pass
        failedDepdency = /2.1|2.2|2.5/.test(testErrors)
        break

      case '2.7':
        // Needs 2.1 pass
        failedDepdency = /2.1/.test(testErrors)
        break

      case '2.8':
        // Needs 2.2 pass
        failedDepdency = /2.2/.test(testErrors)
        break

      default:
        failedDepdency = false
    }

    if (failedDepdency) {
      process.stdout.write(
        `[QE] ===> Skipping test ${test} as dependency failed already ${testErrors}...\n`
      )
    } else {
      process.stdout.write(`[QE] ===> Running test ${test}...\n`)
      await runCypress(test) /* eslint-disable-line no-await-in-loop */
    }
  }
}

// Tear down the environment
async function vtexTearDown() {
  if (NO_VTEX) {
    process.stdout.write(
      `[QE] ===> No VTEX detected, skipping environment deletion...\n`
    )
  } else {
    process.stdout.write(`[QE] ===> Deleting the environment...\n`)
    const BASE_URL = `https://${WORKSPACE()}--sandboxusdev.myvtex.com`
    const CMD =
      `${CYPRESS_BIN} run ${HEADED} -b chrome -P . ` +
      `-e WORKSPACE=${WORKSPACE()},VTEX_BIN=${VTEX_BIN},OTP_SH=${OTP_SH},VTEX_ACCOUNT=${VTEX_ACCOUNT},IN_CYPRESS=true ` +
      `-c baseUrl="${BASE_URL}",testFiles="${CY_TDOWN}" `

    execSync(`${CMD} 2>/dev/null`, { stdio: 'inherit' })
  }
}

async function startCypress() {
  if (ARGV.includes('--open')) {
    process.stdout.write(
      `[QE] ===> Opening with the workspace ${WORKSPACE()}...\n`
    )
    openCypress()
  } else {
    // Set up
    await vtexSetUp()

    // Run tests
    await runTests()

    // Run teardown
    await vtexTearDown()
    // Finish time
    const STOP = Date.now()

    // Report issues
    if (vtexJson.JIRA_CREATE_TICKET) {
      process.stdout.write(
        '[QE] ===> Flag to create Jira ticket is on, looking for errors...\n'
      )
    } else {
      process.stdout.write(
        '[QE] ===> Flag to create Jira ticket is off, skipping it...\n'
      )
    }

    // Shows the time to run all tests
    process.stdout.write(
      `[QE] ===> Time to run all tests: ${(STOP - START) / 1000} seconds.\n`
    )

    // Shows if test success of failed
    const FAILED = `FAILED on ${testErrors.join(', ')} test case(s)`

    process.stdout.write(
      `[QE] ===> The test ${testErrors.length ? FAILED : 'got SUCCEEDED'}!\n`
    )
    process.exit(testErrors.length ? 127 : 0)
  }
}

async function main() {
  // Start VTEX login in background
  vtexBackground()

  // Run startCypress
  await startCypress()
}

main()
