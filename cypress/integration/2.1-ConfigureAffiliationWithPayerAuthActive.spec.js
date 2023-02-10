import { syncCheckoutUICustom } from '../support/common/testcase.js'
import { setWorkspaceInAffiliation } from '../support/affiliation.js'
import { loginViaCookies } from '../support/common/support.js'
import {
  updateCybersourceConfiguration,
  ORDER_SUFFIX,
} from '../support/appSettings.js'

const config = Cypress.env()

// Constants
const { name } = config.workspace

describe('Set up affiliation with payer Auth as active and sync checkout ui custom', () => {
  loginViaCookies()

  setWorkspaceInAffiliation(name)
  syncCheckoutUICustom()
  updateCybersourceConfiguration(ORDER_SUFFIX)
})
