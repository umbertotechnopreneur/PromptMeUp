// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language project-information screen catalog.</summary>
    private static void AddAboutEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("About.Description", new(
            English: "A lightweight, portable terminal assistant. Turn natural-language requests into commands you can review before running.",
            Italian: "Un assistente per il terminale leggero e portatile. Trasforma le richieste in linguaggio naturale in comandi da controllare prima di eseguirli.",
            French: "Un assistant de terminal léger et portable. Transformez vos demandes en langage naturel en commandes à vérifier avant de les exécuter.",
            German: "Ein schlanker, portabler Terminal-Assistent. Formulieren Sie Ihre Anliegen in natürlicher Sprache und prüfen Sie die vorgeschlagenen Befehle vor der Ausführung.",
            Spanish: "Un asistente de terminal ligero y portátil. Convierte tus peticiones en lenguaje natural en comandos que puedes revisar antes de ejecutarlos.",
            Vietnamese: "Trợ lý dòng lệnh gọn nhẹ, có thể mang theo. Biến yêu cầu bằng ngôn ngữ tự nhiên thành các lệnh để bạn xem xét trước khi chạy."));
        entries.Add("About.Author", new(
            English: "Author",
            Italian: "Autore",
            French: "Auteur",
            German: "Autor",
            Spanish: "Autor",
            Vietnamese: "Tác giả"));
        entries.Add("About.Platforms", new(
            English: "Platforms",
            Italian: "Piattaforme",
            French: "Plateformes",
            German: "Plattformen",
            Spanish: "Plataformas",
            Vietnamese: "Nền tảng"));
        entries.Add("About.CloseHint", new(
            English: "Enter / Esc: close",
            Italian: "Invio / Esc: chiudi",
            French: "Entrée / Échap : fermer",
            German: "Enter / Esc: schließen",
            Spanish: "Intro / Esc: cerrar",
            Vietnamese: "Enter / Esc: đóng"));
        entries.Add("About.ScrollHint", new(
            English: "Up/Down / PgUp / PgDn: scroll | Enter / Esc: close",
            Italian: "Su/Giù / PgUp / PgDn: scorri | Invio / Esc: chiudi",
            French: "Haut/Bas / PgUp / PgDn : défiler | Entrée / Échap : fermer",
            German: "Auf/Ab / PgUp / PgDn: scrollen | Enter / Esc: schließen",
            Spanish: "Arriba/Abajo / PgUp / PgDn: desplazar | Intro / Esc: cerrar",
            Vietnamese: "Lên/Xuống / PgUp / PgDn: cuộn | Enter / Esc: đóng"));
        entries.Add("About.ScrollHintCompact", new(
            English: "Up/Down: scroll | Enter/Esc: close",
            Italian: "Su/Giù: scorri | Invio/Esc: chiudi",
            French: "Haut/Bas : défiler | Entrée/Échap : fermer",
            German: "Auf/Ab: scrollen | Enter/Esc: schließen",
            Spanish: "Arriba/Abajo: desplazar | Intro/Esc: cerrar",
            Vietnamese: "Lên/Xuống: cuộn | Enter/Esc: đóng"));
        entries.Add("About.TooSmall", new(
            English: "Enlarge the terminal to at least 60 columns and 20 rows. Enter or Esc closes About.",
            Italian: "Allarga il terminale ad almeno 60 colonne e 20 righe. Invio o Esc chiude le informazioni.",
            French: "Agrandissez le terminal à au moins 60 colonnes et 20 lignes. Entrée ou Échap ferme la page À propos.",
            German: "Vergrößern Sie das Terminal auf mindestens 60 Spalten und 20 Zeilen. Enter oder Esc schließt die Infoseite.",
            Spanish: "Amplía el terminal a un mínimo de 60 columnas y 20 filas. Intro o Esc cierra Acerca de.",
            Vietnamese: "Mở rộng cửa sổ dòng lệnh đến ít nhất 60 cột và 20 dòng. Nhấn Enter hoặc Esc để đóng trang giới thiệu."));
        entries.Add("About.Unavailable", new(
            English: "The About screen is unavailable.",
            Italian: "La schermata delle informazioni non è disponibile.",
            French: "La page À propos n'est pas disponible.",
            German: "Die Infoseite ist nicht verfügbar.",
            Spanish: "La pantalla Acerca de no está disponible.",
            Vietnamese: "Trang giới thiệu hiện không khả dụng."));
        entries.Add("Help.About", new(
            English: "View project information and the HELP ME banner.",
            Italian: "Mostra le informazioni sul progetto e il banner HELP ME.",
            French: "Affiche les informations du projet et la bannière HELP ME.",
            German: "Zeigt Projektinformationen und das HELP ME-Banner an.",
            Spanish: "Muestra la información del proyecto y el banner HELP ME.",
            Vietnamese: "Xem thông tin dự án và dòng chữ nghệ thuật HELP ME."));
    }
}
