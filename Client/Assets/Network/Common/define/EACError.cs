
public enum EACError
{
	AC_NOERROR		= 0,

	/// NULL
	AC_NULL,

	/// 인스턴스 NULL
	AC_INSTANCE_NULL,

	/// 인스턴스 NULL이 아님
	AC_INSTANCE_NOT_NULL,

	/// 핸들러 NULL
	AC_HANDLER_NULL,

	/// 잘못된 Notify
	AC_INVALID_NOTIFY,

	/// 잘못된 Key
	AC_INVALID_KEY,

	/// 잘못된 Value
	AC_INVALID_VALUE,

	/// 잘못된 Instance
	AC_INVALID_INSTANCE,

	/// 잘못된 Count
	AC_INVALID_COUNT,

	/// 객체 생성 실패
	AC_INSTANCE_CREATE_FAILED,

	/// 스테이스 생성 실패
	AC_STATE_CREATE_FAILED,

	/// 스테이스 전환 실패
	AC_STATE_FAILED,

	/// 오브젝트 못 찾음
	AC_OBJECT_NOT_FOUND,

	/// key
	AC_ALREADY_EXIST_KEY,
	AC_NOT_EXIST_KEY,

	/// value
	AC_ALREADY_EXIST_VALUE,
	AC_NOT_EXIST_VALUE,

	/// load
	AC_LOAD_KEY_INVALID,
	AC_LOAD_PATH_INVALID,
	AC_ALREADY_LOADED,
	AC_LOAD_FAILED,

	/// 비었음
	AC_EMPTY,

	/// 없음
	AC_NOT_EXIST,
	AC_NOT_EXIST_ITEM,

	/// 중복
	AC_DUPLICATION,
	AC_DUPLICATION_KEY,
	AC_DUPLICATION_INSTANCE,

	/// 큐가 비었음
	AC_EMPTY_QUEUE,

	/// 로드 오류
	AC_PRELOAD_SEQ_INVALID_SEG,

	/// 프리팹 로드 실패
	AC_PREFAB_LOAD_FAILED,

	/// 알 수 없는 타입
	AC_UNKNOWN_TYPE,

	/// 없는 케이스
	AC_MISSING_CASE,

	/// 파라미터 오류
	AC_INVALID_PARAM,

	/// 이미 추가된 상태
	AC_ALREADY_ADDED_STATE,

	/// 이미 추가된 pawn
	AC_ALREADY_ADDED_PAWN,

	/// 사용금지
	AC_DO_NOT_USED,

	/// 모름
	AC_UNKNOWN_ERROR,

	/// 시스템 다움
	AC_SYSTEM_DOWN,

	/// 위반
	AC_ILLEGAL_OPERATION,

	/// 어플 구동 실패
	AC_FAILED_RUNAPP
}