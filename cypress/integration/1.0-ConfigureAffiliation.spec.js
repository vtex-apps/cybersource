import { syncCheckoutUICustom } from '../support/common/testcase.js'
import { setWorkspaceInAffiliation } from '../support/affiliation.js'
import { loginViaCookies } from '../support/common/support.js'

const config = Cypress.env()

// Constants
const { name } = config.workspace

describe('Set up affiliation and sync checkout ui custom', () => {
  loginViaCookies()

  setWorkspaceInAffiliation(name)
  syncCheckoutUICustom()
})
