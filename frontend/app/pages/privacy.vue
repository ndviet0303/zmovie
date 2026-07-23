<script setup lang="ts">
useHead({ title: 'Chính sách bảo mật — ZMovie' })

const sections = [
  { heading: 'Thông tin chúng tôi thu thập', paragraphs: ['Khi bạn tạo tài khoản hoặc sử dụng ZMovie, chúng tôi có thể nhận thông tin bạn cung cấp như tên và email. Để cải thiện trải nghiệm, dịch vụ cũng ghi nhận dữ liệu kỹ thuật cơ bản và lịch sử xem ở dạng cần thiết cho vận hành.', 'Lượt xem được ghi nhận cùng thời điểm và một mã trình duyệt ẩn danh nhằm tạo các bảng xếp hạng nội dung. Mã này không phải tên, email hay thông tin định danh trực tiếp của bạn.'] },
  { heading: 'Cách chúng tôi sử dụng dữ liệu', paragraphs: ['Dữ liệu được sử dụng để vận hành dịch vụ, duy trì tài khoản, hiển thị tổng lượt xem, xây dựng danh sách thịnh hành và cải thiện độ ổn định của ZMovie.', 'Chúng tôi không bán thông tin cá nhân cho bên thứ ba.'] },
  { heading: 'Lưu trữ và bảo vệ', paragraphs: ['Chúng tôi áp dụng các biện pháp kỹ thuật phù hợp để bảo vệ dữ liệu. Tuy nhiên, không có phương thức truyền tải hoặc lưu trữ nào an toàn tuyệt đối; vì vậy bạn cũng nên bảo mật thông tin đăng nhập của mình.'] },
  { heading: 'Lựa chọn của bạn', paragraphs: ['Bạn có thể yêu cầu xem, chỉnh sửa hoặc xóa thông tin tài khoản của mình theo quy định áp dụng. Bạn cũng có thể xóa dữ liệu trình duyệt để tạo lại mã ẩn danh dùng cho thống kê lượt xem.'] },
  { heading: 'Cập nhật chính sách', paragraphs: ['Chính sách này có thể được điều chỉnh khi sản phẩm hoặc yêu cầu pháp lý thay đổi. Chúng tôi sẽ cập nhật ngày ở đầu trang khi có thay đổi đáng kể.'] },
]
</script>

<template><LegalDocument eyebrow="ZMovie · Quyền riêng tư" title="Chính sách bảo mật" description="Cách ZMovie thu thập, sử dụng và bảo vệ thông tin để trải nghiệm xem phim của bạn luôn rõ ràng." :sections="sections" /></template>
