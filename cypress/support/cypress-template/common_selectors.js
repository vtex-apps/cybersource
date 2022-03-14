export default {
  // *************HomePage Constants start here************ //

  SignInBtn: '.vtex-login-2-x-container > div >button',
  Email: '[class*=emailVerifi] > form > div input[type=text]',
  Password: '[class*=emailVerifi] > form > div input[type=password]',
  PasswordValidation: '.w-80',
  SignInFormBtn: 'div[class*=sendButton] > button',
  SignInLabel: '.vtex-login-2-x-label',
  ProfileIcon: '.vtex-login-2-x-profile',
  LogoutBtn: '[data-test="logout"]',
  Search: 'input[placeholder=Search]',
  OpenCart: 'div[class*=openIcon]',
  MyAccount: '.vtex-login-2-x-accountOptions >div >a >button > span',
  Logout:
    '.vtex-login-2-x-accountOptions >div:last-child > button > div > span',
  ProductDiv: 'article > div',
  ProductRating: 'article >div  div[class*=vtex-reviews-and-ratings]',
  PayPaliframe: 'iframe[name*=paypal]',
  PayPalImg: "button[aria-label*='PayPal Pay Later Message']",
  AddtoCart: 'span[class*=vtex-add-to-cart-button]',
  TotalPrice: '#total-price > .vtex-checkout-summary-0-x-price',
  RemoveProduct: 'div[class*="removeButton"]',
  pickupInStore: '.srp-toggle__pickup',
  ProductsQAShipping: "input[value='Productusqa2']",
  CloseCart: '.vtex-minicart-2-x-closeIconButton',
  // Below products are from sandboxusdev
  AddtoCartBtnForHat: "a[href*='003/p'] > article > button",
  AddtoCartBtnForAdidas: "a[href*='adidas01/p'] > article > button",
  AddtoCartBtnForAdidasv5: "a[href*='adidas01v5/p'] > article > button",
  // Below product is from external seller
  AddtoCartBtnForTShirt: "a[href*='adidas-women'] > article > button",
  // Below products are from productusqa
  AddtoCartBtnForOnion: "a[href*='onion'] > article > button",
  AddtoCartBtnForCoconut: "a[href*='coconut'] > article > button",
  AddtoCartBtnForOrange: "a[href*='frutas'] > article > button",
  AddtoCartBtnForMelon: "a[href*='watermelon'] > article > button",
  // *************HomePage Constants end here************ //

  // *************Search Results Constants end here************ //
  BrandFilter: 'label[for*=brand]',
  ProductAnchorElement: 'section[class*=summary] >a',
  generateAddtoCartSelector: href => {
    return `a[href='${href}'] > article > button`
  },
  searchResult: 'h1[class*=vtex-search]',
  // *************Search Results Constants end here************ //

  // *************Cart Sidebar Constants starts here************ //
  ProductQuantity:
    'div[class*=quantityDropdownContainer] > div > label > div > select',
  Paypal: 'div[class*=paypal]',
  SummaryText: 'span[class*=summarySmall]',
  ProceedtoCheckout: '#proceed-to-checkout',
  NewPayPal: 'div[class*=paypal] > span > iframe[name*=paypal]',
  // *************Cart Sidebar Constants end here************ //

  // *************New Cart - PayPal Constants end here************ //
  ItemQuantity: '#items-quantity',
  // *************New Cart - PayPal Constants end here************ //
  // *************Product Page Constants starts here************ //
  AddressForm: 'div[class*=addressForm]',
  NormalShipping: "input[value='Normal']",
  FilterHeading: 'h5[class*="filter"]',

  // *************Product Page Constants end here************ //

  // *************Order Form Page Constants starts here************ //

  // Progress bar

  CartTimeline: 'span[class*=item_cart]',

  // *************Contact Form Page Constants starts here************ //
  ContactForm: '.form-step.box-edit',
  FirstName: '#client-first-name',
  LastName: '#client-last-name',
  Phone: '#client-phone',
  ProceedtoShipping: '#go-to-shipping',
  GoToPayment: '#go-to-payment',
  // *************Contact Form Page Constants end here************ //

  // *************Shipping Section Constants starts here************ //
  ShippingPreview: '#shipping-preview-container',
  ShippingCalculateLink:
    '#shipping-preview-container > div #shipping-calculate-link',
  ProductQuantityInCheckout: position => {
    return `tr:nth-child(${position}) > div > td.quantity > input`
  },
  ItemRemove: position => {
    return `tr:nth-child(${position}) > div > td.item-remove`
  },
  giftCheckbox: '.available-gift-items > tr > td:nth-child(2) > i',
  PostalCodeFinishedLoading: '#postalCode-finished-loading',
  EditShipping: 'a[id=edit-shipping-data]',
  OpenShipping: '#open-shipping',
  NewAddressBtn: '#new-address-button',
  UpdateSelectedAddressBtn: '#edit-address-button',
  ShippingSectionTitle: 'p[class*=shippingSectionTitle]',
  ShipCountry: '#ship-country',
  ShipStreet: '#ship-street',
  ShipAddressQuery: '#ship-addressQuery',
  ShipCity: '#ship-city',
  ShipState: '#ship-state',
  PostalCodeInput: '#ship-postalCode',
  CalculateBtn: '#cart-shipping-calculate',
  ContinueShipping: '#btn-go-to-shippping-method',
  CalculateShipping: 'button[class*=btnDelivery]',
  ForceShippingFields: '#force-shipping-fields',
  DeliveryAddress: '#deliver-at-text',
  ReceiverName: '#ship-receiverName',
  DeliveryAddressText: '#deliver-at-text > a',

  // *************Shipping Section Constants end here************ //

  // *************Summary Section Constants starts here************ //
  QuantityBadge: '.quantity.badge',
  SummaryCart: 'div[class*=summary-cart] .product-name',
  Discounts: 'tr.Discounts',
  ProceedtoPaymentBtn: 'a[id=cart-to-orderform]',
  ShippingAmtLabel:
    'td[data-i18n="totalizers.Shipping"] ~ td[data-bind="text: valueLabel"]',
  TaxAmtLabel: ".CustomTax > td[data-bind='text: customTaxTotalLabel']",
  TotalLabel: 'td[data-bind="text: totalLabel"]',
  ShippingSummary: 'td[data-bind="text: valueLabel"]',
  GotoPaymentBtn: '#btn-go-to-payment',
  SubTotal:
    '.cart-template > .summary-template-holder > div > .totalizers > div table tr.Items > td.monetary',
  // *************Summary Section Constants end here************ //

  //* ************Payment Section Constants starts here************ //
  ExemptionInput: '[name=tax-exemption__input]',
  SubmitExemption: 'input[class*=tax-exemption__button]',
  VatInput: '[name=vat-number__input]',
  SubmitVat: 'input[class*=vat-number__button]',
  TaxClass: '.CustomTax',
  Net90PaymentBtn: 'a[data-name=Net90]',
  Net90Label: '.payment-description',
  PromissoryPayment: '[data-name=Promissory]',
  BuyNowBtn: '#payment-data-submit > span',
  PaymentConfirmationLabel: '.vtex-order-placed-2-x-confirmationTitle',
  OrderIdLabel: '.vtex-order-placed-2-x-orderNumber',
  PaymentMethodIFrame: '.payment-method iframe',
  CardExist: '#use-another-card',
  CreditCard: 'a[data-name*=American]',
  CreditCardNumber: '[name=cardNumber]',
  CreditCardHolderName: "[name='ccName']",
  CreditCardExpirationMonth: '[name=cardExpirationMonth]',
  CreditCardExpirationYear: '[name=cardExpirationYear]',
  CreditCardCode: '#creditCardpayment-card-0Code',
  PaymentUnAuthorized: 'div[class*=payment-unauthorized]',
  // *************Payment Section Constants end here************ //

  // *************Order Form Page Constants end here************ //
}
