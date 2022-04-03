import {
  singleProduct,
  multiProduct,
  discountProduct,
  discountShipping,
  promotionProduct,
  requestRefund,
} from '../support/sandbox_outputvalidation.js'
import { verifyPaymentSettled } from '../support/testcase.js'

describe('Settlements Test Cases', () => {
  verifyPaymentSettled(singleProduct.prefix, requestRefund.fullRefundEnv)
  verifyPaymentSettled(multiProduct.prefix, requestRefund.partialRefundEnv)
  verifyPaymentSettled(discountProduct.prefix, discountProduct.env)
  verifyPaymentSettled(discountShipping.prefix, discountShipping.env)
  verifyPaymentSettled(promotionProduct.prefix, promotionProduct.env)
})
