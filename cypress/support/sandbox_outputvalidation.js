import { PRODUCTS } from './sandbox_products.js'

export default {
  singleProduct: {
    prefix: 'singleProduct',
    postalCode: '33180',
    totalAmount: '$ 220.42',
    tax: '$ 14.42',
    productPrice: '95.50',
    subTotal: '$ 191.00',
    // singleProduct, taxExemption, vatNumber,multiProduct,refund,externalSeller uses below product
    productName: PRODUCTS.shoesv5,
    productQuantity: '2',
    totalProductPrice: '220.42',
  },
  requestRefund: {
    // orderId variable name for getFullRefundTotal
    fullRefundEnv: 'order1',
    // orderId variable name for getPartialRefund
    partialRefundEnv: 'order2',
    getFullRefundTotal: 108500,
    getPartialRefundTotal: 108500,
    subTotal: '$ 1,080.00',
  },
}
