async function submitImageSearch() {
    const imageInput = document.getElementById('imageSearchInput');
    const imageFile = imageInput.files[0];

    if (!imageFile) {
        showNotification('Vui lòng chọn một hình ảnh để tìm kiếm.', 'error');
        return;
    }

    // Hiển thị loading
    const searchButton = document.querySelector('#imageSearchForm button[type="submit"]');
    const originalText = searchButton.innerHTML;
    searchButton.innerHTML = '<i class="bi bi-hourglass-split"></i> Đang xử lý...';
    searchButton.disabled = true;

    try {
        const formData = new FormData();
        formData.append('imageFile', imageFile);

        console.log('Gửi request tìm kiếm ảnh...');

        const response = await fetch('/Product/SearchByImage', {
            method: 'POST',
            body: formData
        });

        console.log('Response status:', response.status);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('Kết quả:', result);

        if (result.success) {
            // Nếu có redirectUrl, chuyển hướng đến trang search
            if (result.redirectUrl) {
                window.location.href = result.redirectUrl;
            } else if (result.products) {
                // Hiển thị kết quả ngay tại chỗ
                displayImageSearchResults(result);
                showNotification('Tìm kiếm thành công!', 'success');
            }
        } else {
            showNotification(result.message || 'Có lỗi xảy ra khi tìm kiếm.', 'error');
        }
    } catch (error) {
        console.error('Search error:', error);
        showNotification('Lỗi kết nối: ' + error.message, 'error');
    } finally {
        // Khôi phục button
        searchButton.innerHTML = originalText;
        searchButton.disabled = false;
    }
}

function displayImageSearchResults(result) {
    let resultsContainer = document.getElementById('imageSearchResults');
    if (!resultsContainer) {
        resultsContainer = document.createElement('div');
        resultsContainer.id = 'imageSearchResults';
        resultsContainer.className = 'image-search-results mt-4';
        document.querySelector('#imageSearchForm').after(resultsContainer);
    }

    let html = `
        <div class="alert alert-info">
            <h5>Kết quả tìm kiếm cho: <strong>${result.flowerName}</strong></h5>
            <p>Độ chính xác: ${(result.probability * 100).toFixed(1)}%</p>
            <p>Tìm thấy ${result.resultCount} sản phẩm phù hợp</p>
        </div>
    `;

    if (result.products && result.products.length > 0) {
        html += '<div class="row">';
        result.products.forEach(product => {
            const displayPrice = product.discountPrice && product.discountPrice < product.price
                ? product.discountPrice
                : product.price;

            const originalPrice = product.discountPrice && product.discountPrice < product.price
                ? `<small class="text-muted text-decoration-line-through">${formatPrice(product.price)} đ</small>`
                : '';

            html += `
                <div class="col-md-3 mb-4">
                    <div class="card product-card h-100">
                        <img src="${product.imageUrl || '/images/default-product.jpg'}" 
                             class="card-img-top product-img" alt="${product.name}"
                             style="height: 200px; object-fit: cover;">
                        <div class="card-body d-flex flex-column">
                            <h6 class="card-title">${product.name}</h6>
                            <div class="price mt-auto">
                                <span class="text-danger fw-bold">${formatPrice(displayPrice)} đ</span>
                                ${originalPrice}
                            </div>
                            <a href="/Product/Display/${product.id}" class="btn btn-primary btn-sm mt-2">Xem chi tiết</a>
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
    } else {
        html += '<div class="alert alert-warning">Không tìm thấy sản phẩm phù hợp.</div>';
    }

    resultsContainer.innerHTML = html;
}

function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price);
}

function showNotification(message, type = 'info') {
    // Sử dụng toast của Bootstrap
    const toastContainer = document.getElementById('toastContainer') || createToastContainer();

    const toastId = 'toast-' + Date.now();
    const bgClass = type === 'error' ? 'bg-danger' :
        type === 'success' ? 'bg-success' : 'bg-info';

    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    toastContainer.innerHTML += toastHtml;

    // Hiển thị toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement);
    toast.show();

    // Tự động xóa sau khi ẩn
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container position-fixed top-0 end-0 p-3';
    container.style.zIndex = '9999';
    document.body.appendChild(container);
    return container;
}

// Thêm event listener khi trang load
document.addEventListener('DOMContentLoaded', function () {
    const imageSearchForm = document.getElementById('imageSearchForm');
    if (imageSearchForm) {
        imageSearchForm.addEventListener('submit', function (e) {
            e.preventDefault();
            submitImageSearch();
        });
    }
});