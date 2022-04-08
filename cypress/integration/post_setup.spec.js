import {
  configureTaxConfigurationInOrderForm,
  startCyberSource,
} from '../support/orderFormConfiguration.js'
import { setWorkspaceInAffiliation } from '../support/affiliation.js'
import { testSetup } from '../support/common/support.js'

const config = Cypress.env()

// Constants
const { name } = config.workspace

describe('Configure workspace and tax in orderForm configuration', () => {
  testSetup()

  startCyberSource()
  configureTaxConfigurationInOrderForm(name)
  setWorkspaceInAffiliation(name)
})
