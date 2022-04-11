import { verifyPaymentSettled } from '../support/testcase.js'
import { promotionProduct } from '../support/outputvalidation.js'
import { testSetup } from '../support/common/support.js'

describe('Verify settlements for promotional product', () => {
  testSetup()

  verifyPaymentSettled(promotionProduct.prefix, promotionProduct.env)
})