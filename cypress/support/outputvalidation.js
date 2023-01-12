import { PRODUCTS } from './common/utils.js'

export default {
  singleProduct: {
    prefix: 'singleProduct',
    postalCode: '90290',
    totalAmount: '$ 105.10', // $ 114.97
    // We are getting min tax ($ 0.10) due to cybersource test credentials
    // Slack Conversation - https://vtex.slack.com/archives/C02J07NP3JT/p1672762261674729
    tax: '$ 0.10', // $ 9.97
    productPrice: '50.00',
    subTotal: '$ 100.00',
    // singleProduct, taxExemption, vatNumber,multiProduct,refund,externalSeller uses below product
    productName: PRODUCTS.onion,
    productQuantity: '2',
  },
  multiProduct: {
    prefix: 'multiProduct',
    postalCode: '90290',
    pickUpPostalCode: '33180',
    totalAmount: '$ 125.15', // $ 136.87
    tax: '$ 0.15', // $ 11.87
    subTotal: '$ 120.00',
    product1Name: PRODUCTS.onion,
    product2Name: PRODUCTS.waterMelon,
    productQuantity: '2',
    product1Price: '50.00',
    product2Price: '20.00',
  },
  discountProduct: {
    prefix: 'discountProduct',
    postalCode: '90290',
    totalAmount: '$ 95.10', // $ 104.03
    tax: '$ 0.10', // $ 9.03
    subTotal: '$ 100.00',
    productQuantity: '1',
    productPrice: '100.00',
    productName: PRODUCTS.cauliflower,
    env: 'discountProductEnv',
  },

  discountShipping: {
    prefix: 'discountShipping',
    postalCode: '90290',
    totalAmount: '$ 100.10', // $ 109.50
    tax: '$ 0.10', // $ 9.50
    subTotal: '$ 100.00',
    productQuantity: '1',
    productPrice: '100.00',
    shippingLabel: 'Free',
    productName: PRODUCTS.orange,
    env: 'discountShippingOrder',
  },
  promotionProduct: {
    prefix: 'promotionalProduct',
    postalCode: '90290',
    totalAmount: '$ 24.87',
    tax: '$ 0.10',
    subTotal: '$ 39.54',
    discount: '$ -19.77',
    productName: PRODUCTS.greenConventional,
    productQuantity: '2',
    productPrice: '19.77',
    env: 'promotionProductEnv',
  },
  externalSeller: {
    prefix: 'externalSeller',
    postalCode: '90290',
    product1Name: PRODUCTS.onion,
    product2Name: PRODUCTS.tshirt,
    totalAmount: '$ 163.04', // $ 168.17
    tax: '$ 9.04', // $ 14.17
    directSaleAmount: '$ 55.00',
    directSaleEnv: 'directSale',
    directSaleTax: '$ 0.10', // $ 5.23
    externalSellerTax: '$ 0.10',
    externalSellerAmount: '$ 107.94',
    externalSaleEnv: 'externalSaleEnv',
    subTotal: '$ 144.00',
    productQuantity: '1',
    product1Price: '50.00',
    product2Price: '94.00',
  },
  requestRefund: {
    // orderId variable name for getFullRefundTotal
    fullRefundEnv: 'order1',
    // orderId variable name for getPartialRefund
    partialRefundEnv: 'order2',
    getFullRefundTotal: 10510,
    getPartialRefundTotal: 10510,
  },
}
