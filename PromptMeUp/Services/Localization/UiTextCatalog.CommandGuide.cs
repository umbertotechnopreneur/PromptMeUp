// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds offline PDF discovery, explicit opening, and recovery copy in all supported languages.</summary>
    private static void AddCommandGuideEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Guide.Title", new(
            English: "Command guide (PDF)",
            Italian: "Guida ai comandi (PDF)",
            French: "Guide des commandes (PDF)",
            German: "Befehlsübersicht (PDF)",
            Spanish: "Guía de comandos (PDF)",
            Vietnamese: "Hướng dẫn lệnh (PDF)"));
        entries.Add("Guide.Open", new(
            English: "Open the PDF guide",
            Italian: "Apri la guida PDF",
            French: "Ouvrir le guide PDF",
            German: "PDF-Leitfaden öffnen",
            Spanish: "Abrir la guía PDF",
            Vietnamese: "Mở hướng dẫn PDF"));
        entries.Add("Guide.Description", new(
            English: "Two printable pages in English, included with the app and available offline. Find them again in hm --help > Command guide (PDF).",
            Italian: "Due pagine stampabili in inglese, incluse nell’app e disponibili offline. Le ritrovi in hm --help > Guida ai comandi (PDF).",
            French: "Deux pages imprimables en anglais, incluses dans l’application et disponibles hors ligne. Retrouvez-les dans hm --help > Guide des commandes (PDF).",
            German: "Zwei druckbare Seiten auf Englisch, in der App enthalten und offline verfügbar. Erneut öffnen: hm --help > Befehlsübersicht (PDF).",
            Spanish: "Dos páginas imprimibles en inglés, incluidas en la app y disponibles sin conexión. Encuéntralas en hm --help > Guía de comandos (PDF).",
            Vietnamese: "Hai trang tiếng Anh có thể in, đi kèm ứng dụng và dùng ngoại tuyến. Mở lại tại hm --help > Hướng dẫn lệnh (PDF)."));
        entries.Add("Guide.OpenHint", new(
            English: "Enter or Right: open the PDF in your default reader",
            Italian: "Invio o Destra: apri il PDF nel lettore predefinito",
            French: "Entrée ou Droite : ouvrir le PDF dans votre lecteur par défaut",
            German: "Eingabe oder Rechts: PDF im Standardprogramm öffnen",
            Spanish: "Intro o Derecha: abrir el PDF en tu lector predeterminado",
            Vietnamese: "Enter hoặc Phải: mở PDF bằng trình đọc mặc định"));
        entries.Add("Guide.Finish", new(
            English: "Finish",
            Italian: "Termina",
            French: "Terminer",
            German: "Fertig",
            Spanish: "Terminar",
            Vietnamese: "Hoàn tất"));
        entries.Add("Guide.Missing", new(
            English: "The installed command guide is missing: {0}. Repair or reinstall PromptMeUp to restore it.",
            Italian: "La guida ai comandi non è presente nell’installazione: {0}. Ripara o reinstalla PromptMeUp per ripristinarla.",
            French: "Le guide des commandes est absent de l’installation : {0}. Réparez ou réinstallez PromptMeUp pour le restaurer.",
            German: "Die installierte Befehlsübersicht fehlt: {0}. Repariere oder installiere PromptMeUp erneut.",
            Spanish: "Falta la guía de comandos instalada: {0}. Repara o reinstala PromptMeUp para recuperarla.",
            Vietnamese: "Thiếu hướng dẫn lệnh đã cài: {0}. Sửa hoặc cài lại PromptMeUp để khôi phục."));
        entries.Add("Guide.OpenFailed", new(
            English: "The PDF reader could not be started. Open this file manually or configure a default PDF reader: {0}",
            Italian: "Non è stato possibile avviare il lettore PDF. Apri il file manualmente oppure configura un lettore PDF predefinito: {0}",
            French: "Impossible de lancer le lecteur PDF. Ouvrez ce fichier manuellement ou configurez un lecteur PDF par défaut : {0}",
            German: "Der PDF-Reader konnte nicht gestartet werden. Öffne diese Datei manuell oder richte ein PDF-Standardprogramm ein: {0}",
            Spanish: "No se pudo iniciar el lector PDF. Abre este archivo manualmente o configura un lector PDF predeterminado: {0}",
            Vietnamese: "Không thể khởi động trình đọc PDF. Mở tệp này thủ công hoặc đặt trình đọc PDF mặc định: {0}"));
    }
}
