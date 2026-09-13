// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds section names and keyboard guidance for the fullscreen command reference in all supported languages.</summary>
    private static void AddHelpBrowserEntries(Dictionary<string, LocalizedText> entries)
    {
        AddHelpArgumentEntries(entries);
        entries.Add("Help.Browse.Sections", new(
            English: "Sections",
            Italian: "Sezioni",
            French: "Sections",
            German: "Abschnitte",
            Spanish: "Secciones",
            Vietnamese: "Các mục"));
        entries.Add("Help.Browse.Examples", new(
            English: "Examples",
            Italian: "Esempi",
            French: "Exemples",
            German: "Beispiele",
            Spanish: "Ejemplos",
            Vietnamese: "Ví dụ"));
        entries.Add("Help.Browse.Ai", new(
            English: "AI work",
            Italian: "Lavorare con AI",
            French: "Travail avec IA",
            German: "Arbeit mit KI",
            Spanish: "Trabajo con IA",
            Vietnamese: "Làm việc với AI"));
        entries.Add("Help.Browse.Insight", new(
            English: "Status and info",
            Italian: "Stato e info",
            French: "État et infos",
            German: "Status und Info",
            Spanish: "Estado e info",
            Vietnamese: "Thông tin"));
        entries.Add("Help.Browse.Setup", new(
            English: "Settings",
            Italian: "Impostazioni",
            French: "Réglages",
            German: "Einstellungen",
            Spanish: "Configuración",
            Vietnamese: "Thiết lập"));
        entries.Add("Help.Browse.Safety", new(
            English: "Safety",
            Italian: "Sicurezza",
            French: "Sécurité",
            German: "Sicherheit",
            Spanish: "Seguridad",
            Vietnamese: "An toàn"));
        entries.Add("Help.Browse.SectionKeys", new(
            English: "Up/Down: sections | Enter/Right/Tab: commands | Esc/Q: close",
            Italian: "Su/Giù: sezioni | Invio/Destra/Tab: comandi | Esc/Q: chiudi",
            French: "Haut/Bas : sections | Entrée/Droite/Tab : commandes | Esc/Q : fermer",
            German: "Auf/Ab: Abschnitte | Enter/Rechts/Tab: Befehle | Esc/Q: schließen",
            Spanish: "Arriba/Abajo: secciones | Intro/Derecha/Tab: comandos | Esc/Q: cerrar",
            Vietnamese: "Lên/Xuống: mục | Enter/Phải/Tab: lệnh | Esc/Q: đóng"));
        entries.Add("Help.Browse.SectionKeysCompact", new(
            English: "Up/Down: sections | Enter: read | Esc/Q: close",
            Italian: "Su/Giù: sezioni | Invio: leggi | Esc/Q: chiudi",
            French: "Haut/Bas: sections | Entrée: lire | Esc/Q: fermer",
            German: "Auf/Ab: Abschnitte | Enter: lesen | Esc/Q: schließen",
            Spanish: "Arriba/Abajo: secciones | Intro: leer | Esc/Q: cerrar",
            Vietnamese: "Lên/Xuống: mục | Enter: đọc | Esc/Q: đóng"));
        entries.Add("Help.Browse.ScrollKeys", new(
            English: "Up/Down/PgUp/PgDn: scroll | F6/Ctrl+Left: sections | Tab: close action | Esc/Q: close",
            Italian: "Su/Giù/PgUp/PgDn: scorri | F6/Ctrl+Sinistra: sezioni | Tab: pulsante chiudi | Esc/Q: chiudi",
            French: "Haut/Bas/PgUp/PgDn : défiler | F6/Ctrl+Gauche : sections | Tab : bouton fermer | Esc/Q : fermer",
            German: "Auf/Ab/PgUp/PgDn: scrollen | F6/Strg+Links: Abschnitte | Tab: Schließen wählen | Esc/Q: schließen",
            Spanish: "Arriba/Abajo/PgUp/PgDn: desplazar | F6/Ctrl+Izquierda: secciones | Tab: botón cerrar | Esc/Q: cerrar",
            Vietnamese: "Lên/Xuống/PgUp/PgDn: cuộn | F6/Ctrl+Trái: mục | Tab: nút đóng | Esc/Q: đóng"));
        entries.Add("Help.Browse.ScrollKeysCompact", new(
            English: "Up/Down: scroll | F6: sections | Esc/Q: close",
            Italian: "Su/Giù: scorri | F6: sezioni | Esc/Q: chiudi",
            French: "Haut/Bas: défiler | F6: sections | Esc/Q: fermer",
            German: "Auf/Ab: scrollen | F6: Abschnitte | Esc/Q: schließen",
            Spanish: "Arriba/Abajo: desplaza | F6: secciones | Esc/Q: cerrar",
            Vietnamese: "Lên/Xuống: cuộn | F6: mục | Esc/Q: đóng"));
        entries.Add("Help.Browse.Close", new(
            English: "Close",
            Italian: "Chiudi",
            French: "Fermer",
            German: "Schließen",
            Spanish: "Cerrar",
            Vietnamese: "Đóng"));
        entries.Add("Help.Browse.CloseKeys", new(
            English: "Enter/Esc/Q: close | Tab: sections | Shift+Tab: commands",
            Italian: "Invio/Esc/Q: chiudi | Tab: sezioni | Maiusc+Tab: comandi",
            French: "Entrée/Esc/Q : fermer | Tab : sections | Maj+Tab : commandes",
            German: "Enter/Esc/Q: schließen | Tab: Abschnitte | Umschalt+Tab: Befehle",
            Spanish: "Intro/Esc/Q: cerrar | Tab: secciones | Mayús+Tab: comandos",
            Vietnamese: "Enter/Esc/Q: đóng | Tab: mục | Shift+Tab: lệnh"));
        entries.Add("Help.Browse.CloseKeysCompact", new(
            English: "Enter/Esc/Q: close | Tab: sections",
            Italian: "Invio/Esc/Q: chiudi | Tab: sezioni",
            French: "Entrée/Esc/Q: fermer | Tab: sections",
            German: "Enter/Esc/Q: schließen | Tab: Abschnitte",
            Spanish: "Intro/Esc/Q: cerrar | Tab: secciones",
            Vietnamese: "Enter/Esc/Q: đóng | Tab: mục"));
        entries.Add("Help.Browse.Range", new(
            English: "Displayed lines {0}–{1} of {2}",
            Italian: "Righe visualizzate {0}–{1} di {2}",
            French: "Lignes affichées {0}–{1} sur {2}",
            German: "Angezeigte Zeilen {0}–{1} von {2}",
            Spanish: "Líneas mostradas {0}–{1} de {2}",
            Vietnamese: "Các dòng hiển thị {0}–{1} trên {2}"));
        entries.Add("Help.Browse.TooSmall", new(
            English: "Enlarge the terminal to at least 60 columns and 20 rows. Esc or Q closes help.",
            Italian: "Allarga il terminale ad almeno 60 colonne e 20 righe. Esc o Q chiude la guida.",
            French: "Agrandissez le terminal à au moins 60 colonnes et 20 lignes. Esc ou Q ferme l'aide.",
            German: "Vergrößern Sie das Terminal auf mindestens 60 Spalten und 20 Zeilen. Esc oder Q schließt die Hilfe.",
            Spanish: "Amplía el terminal a un mínimo de 60 columnas y 20 filas. Esc o Q cierra la ayuda.",
            Vietnamese: "Mở rộng cửa sổ dòng lệnh đến ít nhất 60 cột và 20 dòng. Nhấn Esc hoặc Q để đóng trợ giúp."));
    }
}
