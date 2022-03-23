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
  promotionProduct: {
    prefix: 'promotionProduct',
    postalCode: '33180',
    totalAmount: '$ 37.21',
    tax: '$ 2.44',
    subTotal: '$ 39.54',
    discount: '$ -19.77',
    productName: PRODUCTS.invicta,
    productQuantity: '2',
    productPrice: '19.77',
    env: 'promotionProductEnv',
  },
  multiProduct: {
    prefix: 'multiProduct',
    postalCode: '33180',
    pickUpPostalCode: '33180',
    totalAmount: '$ 327.42',
    tax: '$ 21.42',
    subTotal: '$ 291.00',
    product1Name: PRODUCTS.shoesv5,
    product2Name: PRODUCTS.shoesv3,
    productQuantity: '2',
    product1Price: '95.50',
    product2Price: '100.00',
  },
  discountShipping: {
    prefix: 'discountShipping',
    postalCode: '90290',
    totalAmount: '$ 109.50',
    tax: '$ 3.82',
    discount:'$ -19.77',
    subTotal: '$ 59.31',
    productQuantity: '1',
    totalWithoutTax: '$ 59.31',
    productPrice: '100.00',
    shippingLabel: 'Free',
    productName: PRODUCTS.invicta,
    env: 'discountShippingOrder',
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
