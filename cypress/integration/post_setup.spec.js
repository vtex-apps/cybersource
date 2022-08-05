import {
  startE2E,
  configureTaxConfigurationInOrderForm,
  syncCheckoutUICustom,
} from '../support/common/testcase.js'
import { setWorkspaceInAffiliation } from '../support/affiliation.js'
import { testSetup } from '../support/common/support.js'

const config = Cypress.env()

// Constants
const { name, prefix } = config.workspace

describe('Configure workspace and tax in orderForm configuration', () => {
  testSetup()

  startE2E(prefix, name)
  configureTaxConfigurationInOrderForm(name)
  setWorkspaceInAffiliation(name)
  syncCheckoutUICustom()
})
