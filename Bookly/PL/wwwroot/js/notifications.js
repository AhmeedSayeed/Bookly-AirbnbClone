const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")   // no accessTokenFactory needed -- the JWT cookie
    .withAutomaticReconnect()          // rides along automatically, same-origin
    .build();

let unreadCount = 0;

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
    toast.innerHTML = `
        <span class="bookly-toast__message">${notification.message}</span>
        ${notification.link ? `<a href="${notification.link}" class="bookly-toast__link">View</a>` : ""}
    `;
    container.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add("bookly-toast--visible"));
    setTimeout(() => {
        toast.classList.remove("bookly-toast--visible");
        toast.addEventListener("transitionend", () => toast.remove(), { once: true });
    }, 5000);
}

document.addEventListener("DOMContentLoaded", initNotifications);