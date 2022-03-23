import selectors from './common/selectors.js'

export function getTestVariables(prefix) {
  return {
    transactionIdEnv: `${prefix}-transactionIdEnv`,
    paymentTransactionIdEnv: `${prefix}-tidEnv`,
  }
}

export function orderAndSaveProductId(
  refundEnv = false,
  externalSeller = false
) {
  // This page take longer time to load. So, wait for profile icon to visible then get orderid from url
  cy.get(selectors.Search, { timeout: 30000 })
  cy.url().then(url => {
    const orderId = `${url.split('=').pop()}-01`

    // If we are ordering product for refund/external seller test case,
    // then store orderId in NodeJS Environment
    if (refundEnv) {
      cy.setOrderItem(refundEnv, orderId)
    }

    if (externalSeller) {
      cy.setOrderItem(externalSeller.directSaleEnv, orderId)
      cy.setOrderItem(
        externalSeller.externalSaleEnv,
        `${orderId.slice(0, -1)}2`
      )
    }
  })
}
