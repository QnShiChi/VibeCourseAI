const GUEST_CART_TOKEN_STORAGE_KEY = "guest-cart-token";

export function getGuestCartToken() {
  return window.localStorage.getItem(GUEST_CART_TOKEN_STORAGE_KEY) ?? "";
}

export function ensureGuestCartToken() {
  const existingToken = getGuestCartToken();
  if (existingToken) {
    return existingToken;
  }

  const nextToken = `guest_${crypto.randomUUID().replace(/-/g, "")}`;
  window.localStorage.setItem(GUEST_CART_TOKEN_STORAGE_KEY, nextToken);
  return nextToken;
}

export function clearGuestCartToken() {
  window.localStorage.removeItem(GUEST_CART_TOKEN_STORAGE_KEY);
}
