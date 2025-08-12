// // wwwroot/js/notification.js
// document.addEventListener('DOMContentLoaded', function() {
//     const notificationIcon = document.getElementById('notificationIcon');
//     const notificationBadge = document.getElementById('notificationBadge');
//     const notificationList = document.getElementById('notificationList');
//     const markAllRead = document.getElementById('markAllRead');

//     // Initialize SignalR connection
//     const connection = new signalR.HubConnectionBuilder()
//         .withUrl("/notificationHub")
//         .configureLogging(signalR.LogLevel.Information)
//         .build();

//     // Start connection
//     connection.start()
//         .then(() => {
//             console.log("SignalR Connected");
//             const userId = document.body.getAttribute('data-user-id');
//             if (userId) {
//                 connection.invoke("JoinNotificationGroup", parseInt(userId));
//             }
//         })
//         .catch(err => console.error(err));

//     // Receive new notifications
//     connection.on("ReceiveNotification", (notification) => {
//         updateNotificationBadge();
//         addNotificationToUI(notification, true);
//     });

//     // Load initial notifications
//     function loadNotifications() {
//         const userId = document.body.getAttribute('data-user-id');
//         if (!userId) return;

//         fetch(`/api/notifications/${userId}`)
//             .then(response => response.json())
//             .then(notifications => {
//                 notificationList.innerHTML = '';
                
//                 if (notifications.length === 0) {
//                     notificationList.innerHTML = '<div class="text-center py-3 text-muted">No notifications</div>';
//                     return;
//                 }
                
//                 notifications.forEach(notification => {
//                     addNotificationToUI(notification);
//                 });
//             });
//     }

//     // Add notification to UI
//     function addNotificationToUI(notification, prepend = false) {
//         const notificationItem = document.createElement('div');
//         notificationItem.className = `dropdown-item ${notification.isRead ? '' : 'bg-light'}`;
//         notificationItem.innerHTML = `
//             <div class="d-flex">
//                 <div class="flex-shrink-0 me-2">
//                     <i class="${notification.iconClass || 'fas fa-bell'} text-primary"></i>
//                 </div>
//                 <div class="flex-grow-1">
//                     <h6 class="mb-1">${notification.title}</h6>
//                     <p class="mb-0 small">${notification.message}</p>
//                     <small class="text-muted">${new Date(notification.createdAt).toLocaleString()}</small>
//                 </div>
//             </div>
//         `;
        
//         notificationItem.addEventListener('click', () => {
//             markNotificationAsRead(notification.id);
//             window.location.href = notification.actionUrl || '#';
//         });

//         if (prepend) {
//             notificationList.prepend(notificationItem);
//         } else {
//             notificationList.appendChild(notificationItem);
//         }
//     }

//     // Update badge count
//     function updateNotificationBadge() {
//         const userId = document.body.getAttribute('data-user-id');
//         if (!userId) return;

//         fetch(`/api/notifications/${userId}/unread-count`)
//             .then(response => response.json())
//             .then(count => {
//                 notificationBadge.textContent = count;
//                 notificationBadge.style.display = count > 0 ? 'block' : 'none';
//             });
//     }

//     // Mark notification as read
//     function markNotificationAsRead(id) {
//         fetch(`/api/notifications/mark-read/${id}`, { method: 'POST' })
//             .then(() => updateNotificationBadge());
//     }

//     // Mark all as read
//     if (markAllRead) {
//         markAllRead.addEventListener('click', function(e) {
//             e.preventDefault();
//             const userId = document.body.getAttribute('data-user-id');
//             if (!userId) return;

//             fetch(`/api/notifications/mark-all-read`, { 
//                 method: 'POST',
//                 headers: {
//                     'Content-Type': 'application/json',
//                 },
//                 body: JSON.stringify({ userId: userId })
//             })
//             .then(() => {
//                 updateNotificationBadge();
//                 loadNotifications();
//             });
//         });
//     }

//     // Initialize
//     updateNotificationBadge();
//     loadNotifications();
// });