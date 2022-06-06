import {
  verifyPaymentSettled,
  paymentAndAPITestCases,
} from '../support/testcase.js'
import { promotionProduct } from '../support/outputvalidation.js'
import { testSetup } from '../support/common/support.js'
import { getTestVariables } from '../support/utils.js'

describe('Verify settlements for promotional product', () => {
  const { prefix, env } = promotionProduct

  testSetup()

  paymentAndAPITestCases(
    null,
    { prefix, approved: true },
    { ...getTestVariables(prefix), orderIdEnv: env }
  )
  verifyPaymentSettled(promotionProduct.prefix, promotionProduct.env)
})
