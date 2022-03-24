export const AUTH_COOKIE_NAME_ENV = 'AUTH_COOKIE_NAME'

// Address list for shipping information
export const addressList = {
  33180: {
    country: 'USA',
    deliveryScreenAddress: '19501 Biscayne Blvd ',
    fullAddress: '  19501 19501 Biscayne Blvd Aventura 33180',
  },
  90290: {
    country: 'USA',
    deliveryScreenAddress: 'Kerry Lane ',
    fullAddress: '  Kerry Ln Topanga 90290',
  },
  93200: {
    country: 'FRA',
    deliveryScreenAddress: '91 Rue de Strasbourg ',
    fullAddress: '  91 Rue de Strasbourg Saint-Denis 93200',
  },
  invalid: {
    country: 'Canada',
    deliveryScreenAddress: '8 Texas Road',
    fullAddress: '8 Texas Road',
  },
}

// VTEX_AUTH_HEADER for API Calls
export const VTEX_AUTH_HEADER = (apiKey, apiToken) => {
  return {
    'X-VTEX-API-AppKey': apiKey,
    'X-VTEX-API-AppToken': apiToken,
  }
}

// We set failOnStatusCode to false
// On failure, this will help us to avoid printing tokens in the screen.
export const FAIL_ON_STATUS_CODE = {
  failOnStatusCode: false,
}

export const ENTITIES = {
  CLIENTS: { id: 'CL', searchKey: 'email' },
  ADDRESSES: { id: 'AD', searchKey: 'userId' },
}