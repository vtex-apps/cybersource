export function getProductName(product) {
  const productList = {
    onion: 'Yellow Onions',
    coconut: 'Fresh Coconuts',
    waterMelon: 'Whole Watermelon',
    orange: 'Navel Oranges',
    cauliflower: 'Cauliflower Fresh',
    tshirt: 'green night',
  }

  return productList[product]
}

export function generateAddtoCartSelector(href) {
  return `a[href='${href}'] > article > button`
}

export function generateAddtoCartCardSelector(href) {
  return `a[href='${href}']`
}

export function addDelayBetweenRetries(delay) {
  if (cy.state('runnable')._currentRetry > 0) cy.wait(delay) // eslint-disable-line cypress/no-unnecessary-waiting
}
