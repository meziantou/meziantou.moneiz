function MoneizLoadDatabase() {
  return localStorage.getItem("moneiz.db");
}

function MoneizSaveDatabase(content) {
  return localStorage.setItem("moneiz.db", content);
}

function MoneizExportDatabase(fileName) {
  const content = MoneizLoadDatabase();

  const file = new File([content], fileName, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl
  a.download = fileName;
  a.click();

  URL.revokeObjectURL(exportUrl);
}

function MoneizImportDatabase(fileName) {
  const content = MoneizLoadDatabase();

  const file = new File([content], fileName, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl
  a.download = fileName;
  a.click();

  URL.revokeObjectURL(exportUrl);
}

function MoneizConfirm(message) {
  return confirm(message);
}

function MoneizAlert(message) {
  return alert(message);
}
