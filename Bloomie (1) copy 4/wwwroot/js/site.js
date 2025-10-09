// site.js - Bloomie main JavaScript file
console.log('Bloomie site.js loaded');

// Các hàm utility chung có thể đặt ở đây
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price);
}

// Xử lý chung cho toàn site
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded');
});