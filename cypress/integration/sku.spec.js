import { updateRetry } from '../support/common/support.js'

describe('Testing Split/Non-Split Sku', () => {
  const tax = '$ 378.70'

  it.skip(
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
})
