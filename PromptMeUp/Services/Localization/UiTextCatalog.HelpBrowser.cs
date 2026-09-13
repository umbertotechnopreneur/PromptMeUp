// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds section names and keyboard guidance for the fullscreen command reference in all supported languages.</summary>
    private static void AddHelpBrowserEntries(Dictionary<string, LocalizedText> entries)
    {
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
            English: "Left/Right, PgUp/PgDn, Tab: sections",
            Italian: "Sinistra/Destra, PgUp/PgDn, Tab: sezioni",
            French: "Gauche/Droite, PgUp/PgDn, Tab : sections",
            German: "Links/Rechts, PgUp/PgDn, Tab: Abschnitte",
            Spanish: "Izquierda/Derecha, PgUp/PgDn, Tab: secciones",
            Vietnamese: "Trái/Phải, PgUp/PgDn, Tab: đổi mục"));
        entries.Add("Help.Browse.ScrollKeys", new(
            English: "Up/Down: scroll | Esc/Q: close",
            Italian: "Su/Giù: scorri | Esc/Q: chiudi",
            French: "Haut/Bas : défiler | Esc/Q : fermer",
            German: "Auf/Ab: scrollen | Esc/Q: schließen",
            Spanish: "Arriba/Abajo: desplazar | Esc/Q: cerrar",
            Vietnamese: "Lên/Xuống: cuộn | Esc/Q: đóng"));
        entries.Add("Help.Browse.Range", new(
            English: "Lines {0}–{1} of {2}",
            Italian: "Righe {0}–{1} di {2}",
            French: "Lignes {0}–{1} sur {2}",
            German: "Zeilen {0}–{1} von {2}",
            Spanish: "Líneas {0}–{1} de {2}",
            Vietnamese: "Dòng {0}–{1} trên {2}"));
        entries.Add("Help.Browse.TooSmall", new(
            English: "Enlarge the terminal to at least 60 columns and 20 rows. Esc or Q closes help.",
            Italian: "Allarga il terminale ad almeno 60 colonne e 20 righe. Esc o Q chiude la guida.",
            French: "Agrandissez le terminal à au moins 60 colonnes et 20 lignes. Esc ou Q ferme l'aide.",
            German: "Vergrößern Sie das Terminal auf mindestens 60 Spalten und 20 Zeilen. Esc oder Q schließt die Hilfe.",
            Spanish: "Amplía el terminal a un mínimo de 60 columnas y 20 filas. Esc o Q cierra la ayuda.",
            Vietnamese: "Mở rộng cửa sổ dòng lệnh đến ít nhất 60 cột và 20 dòng. Nhấn Esc hoặc Q để đóng trợ giúp."));
    }
}
