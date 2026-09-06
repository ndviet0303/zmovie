<script setup lang="ts">
useHead({ title: "Chính sách bản quyền & DMCA — ZMovie" });

const sections = [
  {
    heading: "Tuyên bố bản quyền & Nguồn dữ liệu",
    paragraphs: [
      "ZMovie hoạt động như một công cụ tìm kiếm, tổng hợp và lập chỉ mục nội dung video từ các nguồn phát công khai trên Internet. Chúng tôi không trực tiếp tải lên, lưu trữ hay phát tán bất kỳ tệp video nào vi phạm bản quyền trên máy chủ của mình.",
      "Chúng tôi luôn tôn trọng quyền sở hữu trí tuệ của các nhà sáng tạo và cam kết tuân thủ Đạo luật Bản quyền Kỹ thuật số Thiên niên kỷ (DMCA) cùng các quy định pháp luật sở hữu trí tuệ hiện hành.",
    ],
  },
  {
    heading: "Quy trình gửi thông báo gỡ bỏ (DMCA Takedown Notice)",
    paragraphs: [
      "Nếu bạn là chủ sở hữu quyền tác giả hoặc đại diện được ủy quyền hợp pháp của chủ sở hữu bản quyền và phát hiện nội dung được liên kết qua ZMovie vi phạm quyền tác giả của bạn, vui lòng gửi thông báo chính thức đến đầu mối tiếp nhận khiếu nại của chúng tôi.",
      "Sau khi nhận được thông báo hợp lệ, chúng tôi sẽ tiến hành kiểm tra và xóa bỏ các liên kết hoặc chỉ mục nội dung vi phạm trong vòng 24 đến 48 giờ làm việc.",
    ],
  },
  {
    heading: "Các thông tin bắt buộc trong văn bản khiếu nại",
    paragraphs: [
      "Chữ ký vật lý hoặc điện tử của người được ủy quyền đại diện cho chủ sở hữu bản quyền độc quyền bị cáo buộc vi phạm.",
      "Mô tả cụ thể tác phẩm có bản quyền mà bạn cho rằng đã bị vi phạm, kèm liên kết hoặc giấy chứng nhận sở hữu tác quyền.",
      "Địa chỉ URL chính xác trên trang web ZMovie dẫn đến nội dung bị cáo buộc vi phạm để chúng tôi xác định vị trí tài nguyên.",
      "Thông tin liên hệ đầy đủ của bạn bao gồm: Họ tên, tổ chức/công ty, địa chỉ, số điện thoại và địa chỉ email chính thức.",
      "Tuyên bố thiện chí rằng bạn thực sự tin rằng việc sử dụng tài liệu theo cách bị khiếu nại không được chủ sở hữu bản quyền, đại lý của họ hoặc pháp luật cho phép.",
      "Tuyên bố cam đoan rằng các thông tin trong thông báo là chính xác và chịu trách nhiệm trước pháp luật về việc bạn là chủ sở hữu bản quyền hoặc được ủy quyền hợp pháp.",
    ],
  },
  {
    heading: "Quy trình phản hồi đối chất (Counter-Notification)",
    paragraphs: [
      "Nếu nội dung liên kết của bạn bị gỡ bỏ do nhầm lẫn hoặc nhận dạng sai, bạn có quyền gửi thông báo phản đối tới ZMovie kèm các bằng chứng chứng minh quyền khai thác hợp pháp.",
      "Sau khi nhận được thông báo phản đối hợp lệ, chúng tôi sẽ chuyển tiếp cho bên khiếu nại ban đầu theo đúng quy trình chuẩn của DMCA.",
    ],
  },
  {
    heading: "Đầu mối tiếp nhận khiếu nại bản quyền",
    paragraphs: [
      "Mọi thông báo và văn bản khiếu nại bản quyền vui lòng gửi trực tiếp qua email: dmca@zmovie.local hoặc liên hệ qua form hỗ trợ trực tuyến.",
      "Các yêu cầu gửi thiếu thông tin xác minh quyền sở hữu hoặc không cung cấp liên kết URL cụ thể có thể sẽ bị trì hoãn thời gian xử lý.",
    ],
  },
];
</script>

<template>
  <LegalDocument
    eyebrow="ZMovie · Bản quyền"
    title="Chính sách bản quyền & DMCA"
    description="Quy trình minh bạch và nhanh chóng bảo vệ quyền sở hữu trí tuệ của các tác giả và nhà sản xuất phim."
    :sections="sections"
  />
</template>
