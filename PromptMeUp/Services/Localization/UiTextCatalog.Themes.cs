// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds localized theme metadata, palette semantics, and bundled names for every supported language.</summary>
    private static void AddThemeEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Theme.Metadata.Path", new(
            English: "Theme file",
            Italian: "File del tema",
            French: "Fichier du thème",
            German: "Themendatei",
            Spanish: "Archivo del tema",
            Vietnamese: "Tệp giao diện"));
        entries.Add("Theme.Metadata.Author", new(
            English: "Author",
            Italian: "Autore",
            French: "Auteur",
            German: "Autor",
            Spanish: "Autor",
            Vietnamese: "Tác giả"));
        entries.Add("Theme.Metadata.Website", new(
            English: "Website",
            Italian: "Sito web",
            French: "Site web",
            German: "Website",
            Spanish: "Sitio web",
            Vietnamese: "Trang web"));
        entries.Add("Theme.Metadata.Description", new(
            English: "Description",
            Italian: "Descrizione",
            French: "Description",
            German: "Beschreibung",
            Spanish: "Descripción",
            Vietnamese: "Mô tả"));
        entries.Add("Theme.Metadata.Unavailable", new(
            English: "Not provided",
            Italian: "Non specificato",
            French: "Non renseigné",
            German: "Nicht angegeben",
            Spanish: "No indicado",
            Vietnamese: "Chưa cung cấp"));
        entries.Add("Theme.Semantic.Success", new(
            English: "Success",
            Italian: "Successo",
            French: "Réussite",
            German: "Erfolg",
            Spanish: "Éxito",
            Vietnamese: "Thành công"));
        entries.Add("Theme.Semantic.SuccessHelp", new(
            English: "Action completed",
            Italian: "Operazione riuscita",
            French: "Action réussie",
            German: "Aktion abgeschlossen",
            Spanish: "Acción completada",
            Vietnamese: "Đã hoàn thành"));
        entries.Add("Theme.Semantic.Warning", new(
            English: "Warning",
            Italian: "Attenzione",
            French: "Attention",
            German: "Warnung",
            Spanish: "Atención",
            Vietnamese: "Cảnh báo"));
        entries.Add("Theme.Semantic.WarningHelp", new(
            English: "Needs attention",
            Italian: "Richiede attenzione",
            French: "À vérifier",
            German: "Aufmerksamkeit nötig",
            Spanish: "Requiere atención",
            Vietnamese: "Cần chú ý"));
        entries.Add("Theme.Semantic.Error", new(
            English: "Error",
            Italian: "Errore",
            French: "Erreur",
            German: "Fehler",
            Spanish: "Error",
            Vietnamese: "Lỗi"));
        entries.Add("Theme.Semantic.ErrorHelp", new(
            English: "Action failed",
            Italian: "Operazione fallita",
            French: "Échec de l'action",
            German: "Aktion fehlgeschlagen",
            Spanish: "Acción fallida",
            Vietnamese: "Không thành công"));
        entries.Add("Theme.Ocean", new(
            English: "Ocean",
            Italian: "Oceano",
            French: "Océan",
            German: "Ozean",
            Spanish: "Océano",
            Vietnamese: "Đại dương"));
        entries.Add("Theme.Cobalt", new(
            English: "Cobalt",
            Italian: "Cobalto",
            French: "Cobalt",
            German: "Kobalt",
            Spanish: "Cobalto",
            Vietnamese: "Coban"));
        entries.Add("Theme.Violet", new(
            English: "Violet",
            Italian: "Viola",
            French: "Violet",
            German: "Violett",
            Spanish: "Violeta",
            Vietnamese: "Tím"));
        entries.Add("Theme.Rose", new(
            English: "Rose",
            Italian: "Rosa",
            French: "Rose",
            German: "Rosa",
            Spanish: "Rosa",
            Vietnamese: "Hồng"));
        entries.Add("Theme.Coral", new(
            English: "Coral",
            Italian: "Corallo",
            French: "Corail",
            German: "Koralle",
            Spanish: "Coral",
            Vietnamese: "San hô"));
        entries.Add("Theme.Forest", new(
            English: "Forest",
            Italian: "Foresta",
            French: "Forêt",
            German: "Wald",
            Spanish: "Bosque",
            Vietnamese: "Rừng"));
        entries.Add("Theme.Mint", new(
            English: "Mint",
            Italian: "Menta",
            French: "Menthe",
            German: "Minze",
            Spanish: "Menta",
            Vietnamese: "Bạc hà"));
        entries.Add("Theme.Midnight", new(
            English: "Midnight",
            Italian: "Mezzanotte",
            French: "Minuit",
            German: "Mitternacht",
            Spanish: "Medianoche",
            Vietnamese: "Nửa đêm"));
        entries.Add("Theme.Coffee", new(
            English: "Coffee",
            Italian: "Caffè",
            French: "Café",
            German: "Kaffee",
            Spanish: "Café",
            Vietnamese: "Cà phê"));
        entries.Add("Theme.Graphite", new(
            English: "Graphite",
            Italian: "Grafite",
            French: "Graphite",
            German: "Graphit",
            Spanish: "Grafito",
            Vietnamese: "Than chì"));
    }
}
