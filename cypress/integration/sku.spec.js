import { updateRetry } from '../support/common/support.js'
import { orderTaxAPITestCase } from '../support/testcase.js'
import { promotionProduct } from '../support/outputvalidation.js'

describe('Testing Split/Non-Split Sku', () => {
  // We are getting min tax ($ 0.10) due to cybersource test credentials
  // Slack Conversation - https://vtex.slack.com/archives/C02J07NP3JT/p1672762261674729
  const tax = '$ 0.10'

  it(
    'Verifying same sku with split of 10 products(7 and 3) then verify tax amounts via order-tax API',
    updateRetry(5),
    () => {
      // We have stored request payload in skuSplit.json of cypress/fixtures folder
      // That we are loading using cy.fixture() then we are calling orderTax API with the payload
      cy.fixture('skuSplit').then(requestPayload =>
        cy.orderTaxApi(requestPayload, tax)
      )
    }
  )

  it(
    'Verifying same sku with non-split of 10 products then verify tax amounts via order-tax API',
    updateRetry(5),
    () => {
      // We have stored request payload in skuNonSplit.json of cypress/fixtures
      // That we are loading using cy.fixture() then we are calling orderTax API with the payload
      cy.fixture('skuNonSplit').then(requestPayload =>
        cy.orderTaxApi(requestPayload, tax)
      )
    }
  )

  // Verify tax via order tax api
  // When we run promotional product orderTaxAPI Testcase
  // inside 2.5 it is causing some tax communication error
  // So, moved to this file
  // Sample runs - https://github.com/vtex-apps/cybersource/actions/runs/3910214324/jobs/6682215203
  orderTaxAPITestCase(promotionProduct.prefix, tax)
})
