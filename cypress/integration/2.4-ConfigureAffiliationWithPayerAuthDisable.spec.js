import { setWorkspaceInAffiliation } from '../support/affiliation.js'
import { loginViaCookies } from '../support/common/support.js'
import { updateCybersourceConfiguration } from '../support/appSettings.js'

const config = Cypress.env()

// Constants
const { name } = config.workspace

describe('Set up affiliation with payer Auth as disabled', () => {
  loginViaCookies()

  setWorkspaceInAffiliation(name, false)
  updateCybersourceConfiguration()
})
