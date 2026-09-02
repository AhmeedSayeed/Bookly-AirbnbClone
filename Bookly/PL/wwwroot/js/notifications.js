const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")   // no accessTokenFactory needed -- the JWT cookie
    .withAutomaticReconnect()          // rides along automatically, same-origin
    .build();

let unreadCount = 0;

function resolveNotificationText(notification) {
    if (notification.messageKey && window.booklyNotificationTemplates) {
        const template = window.booklyNotificationTemplates[notification.messageKey];

        if (template) {
            let text = template;

            if (notification.messageArgsJson) {
                try {
                    const args = JSON.parse(notification.messageArgsJson);

                    args.forEach((arg, index) => {
                        text = text.replace(
                            new RegExp("\\{" + index + "\\}", "g"),
                            arg ?? ""
                        );
                    });
                } catch {
                    // Keep the template unchanged if args cannot be parsed
                }
            }

            return text;
        }
    }

    return notification.legacyMessage || "";
}

function getLocalizedViewText() {
    return window.booklyI18n?.view || "View";
}

async function initNotifications() {
    const res = await fetch("/Notifications/UnreadCount");
    unreadCount = await res.json();
    updateBadge();

    connection.on("ReceiveNotification", (notification) => {
        unreadCount++;
        updateBadge();
        showToast(notification);
    });

    await connection.start();
}

function updateBadge() {
    const badge = document.getElementById("notificationBadge");
    badge.style.display = unreadCount > 0 ? "flex" : "none";
    badge.textContent = unreadCount > 9 ? "9+" : unreadCount;
}

function showToast(notification) {
    const container = document.getElementById("toastContainer");
    const toast = document.createElement("div");
    toast.className = "bookly-toast";

    const message = resolveNotificationText(notification);
    const viewText = getLocalizedViewText();

    toast.innerHTML = `
        <span class="bookly-toast__message">${message}</span>
        ${notification.link ? `<a href="${notification.link}" class="bookly-toast__link">${viewText}</a>` : ""}
    `;

    container.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add("bookly-toast--visible"));

    setTimeout(() => {
        toast.classList.remove("bookly-toast--visible");
        toast.addEventListener("transitionend", () => toast.remove(), { once: true });
    }, 5000);
}

document.addEventListener("DOMContentLoaded", initNotifications);