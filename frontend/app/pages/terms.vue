<script setup lang="ts">
useHead({ title: 'Điều khoản sử dụng — ZMovie' })

const sections = [
  { heading: 'Chấp nhận điều khoản', paragraphs: ['Khi truy cập hoặc sử dụng ZMovie, bạn đồng ý tuân thủ các điều khoản này và những quy định pháp luật hiện hành. Nếu không đồng ý, vui lòng không sử dụng dịch vụ.'] },
  { heading: 'Tài khoản của bạn', paragraphs: ['Bạn chịu trách nhiệm bảo mật thông tin đăng nhập và các hoạt động diễn ra trên tài khoản của mình. Hãy thông báo cho chúng tôi khi phát hiện việc sử dụng trái phép.', 'Bạn cần cung cấp thông tin chính xác khi tạo tài khoản và không được mạo danh người khác.'] },
  { heading: 'Sử dụng dịch vụ', paragraphs: ['ZMovie cung cấp trải nghiệm khám phá và xem nội dung điện ảnh. Bạn không được can thiệp vào hoạt động của dịch vụ, sao chép trái phép nội dung, phát tán mã độc hoặc sử dụng dịch vụ cho mục đích vi phạm pháp luật.', 'Quyền truy cập có thể bị giới hạn, tạm ngừng hoặc chấm dứt khi cần thiết để bảo vệ người dùng, hệ thống hoặc theo yêu cầu pháp lý.'] },
  { heading: 'Nội dung và quyền sở hữu', paragraphs: ['Giao diện, nhãn hiệu và các thành phần do ZMovie cung cấp được bảo vệ bởi quyền sở hữu trí tuệ. Nội dung từ nguồn bên thứ ba có thể chịu các điều khoản và quyền riêng của bên cung cấp đó.'] },
  { heading: 'Thay đổi điều khoản', paragraphs: ['Chúng tôi có thể cập nhật điều khoản để phản ánh thay đổi của dịch vụ hoặc yêu cầu pháp lý. Phiên bản mới sẽ được công bố trên trang này cùng ngày cập nhật.'] },
]
</script>

<template><LegalDocument eyebrow="ZMovie · Pháp lý" title="Điều khoản sử dụng" description="Các quy tắc giúp mọi người tận hưởng ZMovie một cách an toàn, tôn trọng và minh bạch." :sections="sections" /></template>
