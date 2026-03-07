function showAlert(icon, title) {
    const Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        didOpen: (toast) => {
            toast.onmouseenter = Swal.stopTimer;
            toast.onmouseleave = Swal.resumeTimer;
        }
    });
    Toast.fire({
        icon: icon,
        title: title
    });
}

function showConfirmationDialog(text, onConfirm) {
    Swal.fire({
        title: "Are you sure?",
        text: text,
        icon: "warning",
        showCancelButton: true,
    }).then((result) => {
        if (result.isConfirmed) {
            onConfirm();
        }
    });
}