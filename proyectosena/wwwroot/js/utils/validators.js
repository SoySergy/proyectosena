export function isEmailValid(email) {
    return /\S+@\S+\.\S+/.test(email);
}

export function isMinLength(value, min) {
    return value.length >= min;
}