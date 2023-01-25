import {
  requestRefund,
  singleProduct,
  externalSeller,
} from '../support/outputvalidation'
import { loginViaCookies } from '../support/common/support.js'
import { refund } from '../support/common/refund_apis.js'
import { getRefundPayload } from '../support/refund_payload.js'
import { getTestVariables } from '../support/utils.js'
import { verifyRefundTid } from '../support/testcase.js'

describe('Testing Cybersource transaction API for full refund', () => {
  // Load test setup
  loginViaCookies()

  const { prefix } = singleProduct
  const { transactionIdEnv, paymentTransactionIdEnv } = getTestVariables(prefix)

  it('Verify whether we have an order to request for full refund', () => {
    cy.getOrderItems().then(order => {
      if (!order[requestRefund.fullRefundEnv] && !order[transactionIdEnv]) {
        throw new Error('SingleProduct Order id/Transaction id is missing')
      }
    })
  })

  // Request full refund for the ordered product added in 2.1-singleProduct.spec.js
  refund(
    {
      total: requestRefund.getFullRefundTotal, // Amount
      title: 'full', // Refund Type for test case title
      env: requestRefund.fullRefundEnv, // variable name where we stored the orderid in node environment
      externalSeller,
    },
    getRefundPayload,
    { sendInvoice: true, startHandling: true }
  )

  // verify cybersource transaction
  verifyRefundTid({
    prefix: 'fullRefund',
    paymentTransactionIdEnv,
    payerAuth: true,
  })
})
