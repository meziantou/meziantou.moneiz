function MoneizLoadDatabase() {
    return localStorage.getItem("moneiz.db");
}

function MoneizSaveDatabase(content) {
    return localStorage.setItem("moneiz.db", content);
}

function MoneizConfirm(message) {
    return confirm(message);
}