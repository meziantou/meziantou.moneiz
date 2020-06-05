const MoneizLocalStorageName = "moneiz.db";
const MoneizDbFileName = "moneiz.moneizdb";

function MoneizLoadDatabase() {
  return localforage.getItem(MoneizLocalStorageName);
}

function MoneizSaveDatabase(content) {
  return localforage.setItem(MoneizLocalStorageName, content);
}

async function MoneizExportDatabase() {
  const content = await MoneizLoadDatabase();

  const file = new File([content], MoneizDbFileName, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = MoneizDbFileName;
  a.target = "_self";
  a.click();
  //URL.revokeObjectURL(exportUrl); doesn't work with Safari
}

function MoneizConfirm(message) {
  return confirm(message);
}

function MoneizAlert(message) {
  return alert(message);
}
