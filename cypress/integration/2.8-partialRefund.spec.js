import { requestRefund, multiProduct } from '../support/outputvalidation'
import { testSetup } from '../support/common/support.js'
import { refund } from '../support/common/refund_apis.js'
import { getRefundPayload } from '../support/refund_payload.js'
import { getTestVariables } from '../support/utils.js'
import { verifyRefundTid } from '../support/testcase.js'

describe('Testing Cybersource transaction API for partial refund', () => {
  // Load test setup
  testSetup()

  const { prefix } = multiProduct
  const { transactionIdEnv, paymentTransactionIdEnv } = getTestVariables(prefix)

  it('Verify whether we have an order to request for partial refund', () => {
    cy.getOrderItems().then(order => {
      if (!order[requestRefund.partialRefundEnv] && !order[transactionIdEnv]) {
        throw new Error('MultiProduct Order id/Transaction id is missing')
      }
    })
  })

  // Request partial refund for the ordered product added in 2.2-multiproduct.spec.js
  refund(
    {
      total: requestRefund.getPartialRefundTotal, // Amount
      title: 'partial', // Refund Type for test case title
      env: requestRefund.partialRefundEnv, // variable name where we stored the orderid in node environment
    },
    getRefundPayload
  )

  // verify cybersource transaction
  verifyRefundTid({
    prefix: 'partialRefund',
    paymentTransactionIdEnv,
  })
})
