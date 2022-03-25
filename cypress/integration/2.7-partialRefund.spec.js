import {
  requestRefund,
  multiProduct,
} from '../support/sandbox_outputvalidation.js'
import { testSetup } from '../support/common/support.js'
import { refund } from '../support/sandbox_refund_apis.js'
import { getRefundPayload } from '../support/sandbox_refund.js'
import { getTestVariables } from '../support/utils.js'
import { verifyRefundTid } from '../support/testcase.js'

describe('Testing Cybersource transaction API for partial refund', () => {
  // Load test setup
  testSetup()

  const { prefix } = multiProduct
  const { paymentTransactionIdEnv } = getTestVariables(prefix)

  it('Verify whether we have an order to request for full refund', () => {
    cy.getOrderItems().then(order => {
      if (!order[requestRefund.partialRefundEnv]) {
        throw new Error('Order id is missing')
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
