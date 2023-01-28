import {
  paymentTestCases,
  APITestCases,
  verifyCyberSourceAPI,
} from '../support/testcase.js'
import { loginViaCookies } from '../support/common/support.js'
import { getTestVariables } from '../support/utils.js'
import {
  singleProduct,
  multiProduct,
  discountProduct,
  discountShipping,
  requestRefund,
  externalSeller,
  promotionProduct,
} from '../support/outputvalidation.js'

describe('Verify settlements for ordered products', () => {
  loginViaCookies()

  // SingleProduct API testcase
  APITestCases(
    { prefix: singleProduct.prefix, approved: true },
    {
      ...getTestVariables(singleProduct.prefix),
      orderIdEnv: requestRefund.fullRefundEnv,
    }
  )

  // MultiProduct API testcase
  APITestCases(
    { prefix: multiProduct.prefix, approved: true },
    {
      ...getTestVariables(multiProduct.prefix),
      orderIdEnv: requestRefund.partialRefundEnv,
    }
  )

  // discountProduct
  APITestCases(
    { prefix: discountProduct.prefix, approved: true },
    {
      ...getTestVariables(discountProduct.prefix),
      orderIdEnv: discountProduct.env,
    }
  )

  // discountShipping
  APITestCases(
    { prefix: discountShipping.prefix, approved: true },
    {
      ...getTestVariables(discountShipping.prefix),
      orderIdEnv: discountShipping.env,
    }
  )

  const { transactionIdEnv, paymentTransactionIdEnv } = getTestVariables(
    externalSeller.prefix
  )

  // externalSeller
  verifyCyberSourceAPI({
    prefix: externalSeller.prefix,
    transactionIdEnv,
    paymentTransactionIdEnv,
    approved: true,
  })

  paymentTestCases(
    null,
    { prefix: promotionProduct.prefix, approved: true },
    {
      ...getTestVariables(promotionProduct.prefix),
      orderIdEnv: promotionProduct.env,
    }
  )
  APITestCases(
    { prefix: promotionProduct.prefix, approved: true },
    {
      ...getTestVariables(promotionProduct.prefix),
      orderIdEnv: promotionProduct.env,
    }
  )
})
