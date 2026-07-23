"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from "@microsoft/signalr";
import { api } from "@/lib/api";

type ChatRoomType =
    | "Private"
    | "Support"
    | "Group"
    | "Project"
    | "Task";

type ChatRoomMemberRole = "Owner" | "Admin" | "Member";

type ChatMessageType = "Text" | "Image" | "File" | "System";

type CreateMode =
    | "private"
    | "support"
    | "group"
    | "project"
    | "task";

type ConnectionStatus =
    | "connecting"
    | "connected"
    | "reconnecting"
    | "disconnected";

type ChatUser = {
    id: string;
    fullName: string;
    email: string;
    role: string;
};

type ChatMember = {
    userId: string;
    fullName: string;
    avatarUrl?: string | null;
    role: ChatRoomMemberRole;
    isOnline: boolean;
};

type ChatMessage = {
    id: string;
    roomId: string;
    senderId: string;
    senderName: string;
    content: string;
    messageType: ChatMessageType;
    attachmentUrl?: string | null;
    replyToMessage?: ChatMessage | null;
    createdAt: string;
    editedAt?: string | null;
    isDeleted: boolean;
    readBy: Array<{
        userId: string;
        fullName: string;
        readAt: string;
    }>;
};

type ChatRoom = {
    id: string;
    name?: string | null;
    type: ChatRoomType;
    lastMessage?: ChatMessage | null;
    unreadCount: number;
    members: ChatMember[];
    createdAt: string;
};

type ProjectOption = {
    id: string;
    name: string;
};

type TaskOption = {
    id: string;
    title: string;
};

type TaskListResponse = {
    items?: TaskOption[];
};

const apiBaseUrl = "http://localhost:5070";

const createModes: Array<{ id: CreateMode; label: string }> = [
    { id: "private", label: "Cá nhân" },
    { id: "group", label: "Nhóm" },
    { id: "support", label: "Hỗ trợ" },
    { id: "project", label: "Dự án" },
    { id: "task", label: "Công việc" },
];

const roomTypeLabels: Record<ChatRoomType, string> = {
    Private: "Cá nhân",
    Support: "Hỗ trợ",
    Group: "Nhóm",
    Project: "Dự án",
    Task: "Công việc",
};

const memberRoleLabels: Record<ChatRoomMemberRole, string> = {
    Owner: "Chủ phòng",
    Admin: "Quản trị",
    Member: "Thành viên",
};

const getToken = () =>
    typeof window === "undefined"
        ? ""
        : localStorage.getItem("token") || "";

const readJwtPayload = () => {
    const token = getToken();

    if (!token) {
        return {};
    }

    try {
        const payload = token.split(".")[1];
        const normalized = payload
            .replace(/-/g, "+")
            .replace(/_/g, "/");
        const padded = normalized.padEnd(
            Math.ceil(normalized.length / 4) * 4,
            "="
        );

        return JSON.parse(window.atob(padded)) as Record<string, unknown>;
    } catch {
        return {};
    }
};

const readClaim = (
    payload: Record<string, unknown>,
    keys: string[]
) => {
    for (const key of keys) {
        const value = payload[key];

        if (typeof value === "string" && value.trim()) {
            return value;
        }
    }

    return "";
};

const getCurrentUserInfo = () => {
    const payload = readJwtPayload();

    return {
        id: readClaim(payload, [
            "nameid",
            "sub",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        ]),
        name:
            (typeof window === "undefined"
                ? ""
                : localStorage.getItem("userName") || "") ||
            readClaim(payload, [
                "name",
                "unique_name",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            ]),
        role:
            (typeof window === "undefined"
                ? ""
                : localStorage.getItem("role") || "") ||
            readClaim(payload, [
                "role",
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            ]) ||
            "User",
    };
};

const sameId = (left?: string | null, right?: string | null) =>
    Boolean(left && right && left.toLowerCase() === right.toLowerCase());

const getInitials = (name?: string | null) => {
    const normalized = (name || "Chat").trim();
    const parts = normalized
        .split(/\s+/)
        .filter(Boolean)
        .slice(-2);

    return parts
        .map((part) => part[0])
        .join("")
        .toUpperCase();
};

const getRoomTitle = (room?: ChatRoom | null) => {
    if (!room) {
        return "";
    }

    return room.name?.trim() || roomTypeLabels[room.type] || "Tin nhắn";
};

const getRoomSubtitle = (room: ChatRoom) =>
    `${roomTypeLabels[room.type]} - ${room.members.length} thành viên`;

const getRoomAvatarClass = (type: ChatRoomType) => {
    switch (type) {
        case "Private":
            return "bg-[#0068ff]";
        case "Support":
            return "bg-emerald-500";
        case "Group":
            return "bg-violet-500";
        case "Project":
            return "bg-amber-500";
        case "Task":
            return "bg-rose-500";
        default:
            return "bg-slate-500";
    }
};

const formatConversationTime = (value?: string | null) => {
    if (!value) {
        return "";
    }

    const date = new Date(value);
    const now = new Date();
    const today = new Date(now);
    const yesterday = new Date(now);

    today.setHours(0, 0, 0, 0);
    yesterday.setHours(0, 0, 0, 0);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date >= today) {
        return date.toLocaleTimeString("vi-VN", {
            hour: "2-digit",
            minute: "2-digit",
        });
    }

    if (date >= yesterday) {
        return "Hôm qua";
    }

    return date.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
    });
};

const formatMessageTime = (value: string) =>
    new Date(value).toLocaleTimeString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
    });

const formatMessageDay = (value: string) => {
    const date = new Date(value);
    const now = new Date();
    const today = new Date(now);
    const yesterday = new Date(now);

    today.setHours(0, 0, 0, 0);
    yesterday.setHours(0, 0, 0, 0);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date >= today) {
        return "Hôm nay";
    }

    if (date >= yesterday) {
        return "Hôm qua";
    }

    return date.toLocaleDateString("vi-VN", {
        weekday: "long",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    });
};

const isSameDay = (left: string, right: string) => {
    const leftDate = new Date(left);
    const rightDate = new Date(right);

    return (
        leftDate.getFullYear() === rightDate.getFullYear() &&
        leftDate.getMonth() === rightDate.getMonth() &&
        leftDate.getDate() === rightDate.getDate()
    );
};

const getLastMessagePreview = (room: ChatRoom) => {
    const message = room.lastMessage;

    if (!message) {
        return "Chưa có tin nhắn";
    }

    if (message.isDeleted) {
        return `${message.senderName}: Tin nhắn đã thu hồi`;
    }

    if (message.attachmentUrl && !message.content) {
        return `${message.senderName}: Đã gửi tệp đính kèm`;
    }

    return `${message.senderName}: ${message.content}`;
};

const inferMessageType = (
    content: string,
    attachmentUrl: string
): ChatMessageType => {
    if (!attachmentUrl.trim()) {
        return "Text";
    }

    if (/\.(png|jpe?g|gif|webp|bmp|svg)(\?.*)?$/i.test(attachmentUrl)) {
        return "Image";
    }

    return content.trim() ? "Text" : "File";
};

const getApiErrorMessage = (error: unknown, fallback: string) => {
    const maybeError = error as {
        response?: {
            data?: unknown;
        };
    };

    if (typeof maybeError.response?.data === "string") {
        return maybeError.response.data;
    }

    return fallback;
};

export default function ChatPage() {
    const currentUser = useMemo(() => getCurrentUserInfo(), []);
    const currentUserId = currentUser.id;
    const isCurrentUserAdmin = currentUser.role === "Admin";

    const connectionRef = useRef<HubConnection | null>(null);
    const selectedRoomRef = useRef<string>("");
    const messagesEndRef = useRef<HTMLDivElement | null>(null);
    const typingTimerRef = useRef<number | null>(null);

    const [rooms, setRooms] = useState<ChatRoom[]>([]);
    const [users, setUsers] = useState<ChatUser[]>([]);
    const [projects, setProjects] = useState<ProjectOption[]>([]);
    const [tasks, setTasks] = useState<TaskOption[]>([]);
    const [selectedRoomId, setSelectedRoomId] = useState("");
    const [messages, setMessages] = useState<ChatMessage[]>([]);

    const [messageText, setMessageText] = useState("");
    const [attachmentUrl, setAttachmentUrl] = useState("");
    const [showAttachmentInput, setShowAttachmentInput] = useState(false);
    const [replyToMessage, setReplyToMessage] =
        useState<ChatMessage | null>(null);
    const [editingMessage, setEditingMessage] =
        useState<ChatMessage | null>(null);

    const [typingText, setTypingText] = useState("");
    const [searchText, setSearchText] = useState("");
    const [createMode, setCreateMode] = useState<CreateMode>("private");
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isDetailsOpen, setIsDetailsOpen] = useState(false);

    const [groupName, setGroupName] = useState("");
    const [groupMemberIds, setGroupMemberIds] = useState<string[]>([]);
    const [privateUserId, setPrivateUserId] = useState("");
    const [supportAdminId, setSupportAdminId] = useState("");
    const [projectId, setProjectId] = useState("");
    const [taskId, setTaskId] = useState("");
    const [memberToAdd, setMemberToAdd] = useState("");
    const [roomNameDraft, setRoomNameDraft] = useState("");

    const [isLoading, setIsLoading] = useState(true);
    const [isLoadingMessages, setIsLoadingMessages] = useState(false);
    const [isSending, setIsSending] = useState(false);
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("connecting");
    const [errorMessage, setErrorMessage] = useState("");

    const selectedRoom = useMemo(
        () => rooms.find((room) => room.id === selectedRoomId),
        [rooms, selectedRoomId]
    );

    const filteredRooms = useMemo(() => {
        const keyword = searchText.trim().toLowerCase();

        if (!keyword) {
            return rooms;
        }

        return rooms.filter((room) => {
            const title = getRoomTitle(room).toLowerCase();
            const preview = getLastMessagePreview(room).toLowerCase();

            return title.includes(keyword) || preview.includes(keyword);
        });
    }, [rooms, searchText]);

    const addableUsers = useMemo(() => {
        const memberIds = new Set(
            selectedRoom?.members.map((member) =>
                member.userId.toLowerCase()
            ) ?? []
        );

        return users.filter(
            (user) => !memberIds.has(user.id.toLowerCase())
        );
    }, [selectedRoom?.members, users]);

    const onlineMembers = useMemo(
        () =>
            selectedRoom?.members.filter((member) => member.isOnline) ??
            [],
        [selectedRoom?.members]
    );

    const fetchRooms = useCallback(async () => {
        const res = await api.get<ChatRoom[]>("/api/chat/rooms");

        return res.data;
    }, []);

    const refreshRooms = useCallback(async () => {
        const data = await fetchRooms();

        setRooms(data);
        return data;
    }, [fetchRooms]);

    const fetchMessages = useCallback(async (roomId: string) => {
        const res = await api.get<ChatMessage[]>(
            `/api/chat/rooms/${roomId}/messages`,
            {
                params: {
                    page: 1,
                    pageSize: 80,
                },
            }
        );

        return res.data;
    }, []);

    const markAsRead = useCallback(async (roomId: string) => {
        const connection = connectionRef.current;

        if (
            connection &&
            connection.state === HubConnectionState.Connected
        ) {
            await connection.invoke("MarkAsRead", roomId);
            return;
        }

        await api.post(`/api/chat/rooms/${roomId}/read`);
    }, []);

    useEffect(() => {
        let ignore = false;

        const load = async () => {
            setIsLoading(true);
            setErrorMessage("");

            try {
                const [roomData, userData, projectData, taskData] =
                    await Promise.all([
                        fetchRooms(),
                        api.get<ChatUser[]>("/api/chat/users"),
                        api.get<ProjectOption[]>("/api/projects"),
                        api.get<TaskListResponse | TaskOption[]>(
                            "/api/tasks",
                            {
                                params: {
                                    page: 1,
                                    pageSize: 80,
                                },
                            }
                        ),
                    ]);

                if (ignore) {
                    return;
                }

                const taskItems = Array.isArray(taskData.data)
                    ? taskData.data
                    : taskData.data.items ?? [];

                setRooms(roomData);
                setUsers(userData.data);
                setProjects(projectData.data);
                setTasks(taskItems);

                if (!selectedRoomRef.current) {
                    const firstRoomId = roomData[0]?.id ?? "";
                    setSelectedRoomId(firstRoomId);
                    setRoomNameDraft(roomData[0]?.name ?? "");
                }
            } catch (error) {
                console.log(error);

                if (!ignore) {
                    setErrorMessage(
                        getApiErrorMessage(
                            error,
                            "Không tải được dữ liệu chat. Kiểm tra đăng nhập và backend."
                        )
                    );
                }
            } finally {
                if (!ignore) {
                    setIsLoading(false);
                }
            }
        };

        void load();

        return () => {
            ignore = true;
        };
    }, [fetchRooms]);

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl(`${apiBaseUrl}/hubs/chat`, {
                accessTokenFactory: () => getToken(),
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        connection.onreconnecting(() => {
            setConnectionStatus("reconnecting");
        });

        connection.onreconnected(() => {
            setConnectionStatus("connected");
            void refreshRooms();

            const roomId = selectedRoomRef.current;
            if (roomId) {
                connection.invoke("JoinRoom", roomId).catch(console.log);
            }
        });

        connection.onclose(() => {
            setConnectionStatus("disconnected");
        });

        connection.on("ReceiveMessage", (message: ChatMessage) => {
            if (message.roomId === selectedRoomRef.current) {
                setMessages((current) => {
                    if (
                        current.some((item) => item.id === message.id)
                    ) {
                        return current;
                    }

                    return [...current, message];
                });

                if (!sameId(message.senderId, currentUserId)) {
                    void markAsRead(message.roomId);
                }
            }

            void refreshRooms();
        });

        connection.on("MessageUpdated", (message: ChatMessage) => {
            setMessages((current) =>
                current.map((item) =>
                    item.id === message.id ? message : item
                )
            );
            void refreshRooms();
        });

        connection.on("MessageDeleted", (message: ChatMessage) => {
            setMessages((current) =>
                current.map((item) =>
                    item.id === message.id ? message : item
                )
            );
            void refreshRooms();
        });

        connection.on(
            "UserTyping",
            (payload: { roomId: string; userName?: string }) => {
                if (payload.roomId !== selectedRoomRef.current) {
                    return;
                }

                if (typingTimerRef.current) {
                    window.clearTimeout(typingTimerRef.current);
                }

                setTypingText(
                    `${payload.userName || "Ai đó"} đang nhập...`
                );
                typingTimerRef.current = window.setTimeout(
                    () => setTypingText(""),
                    1800
                );
            }
        );

        connection.on(
            "MessagesRead",
            (payload: { roomId: string }) => {
                void refreshRooms();

                if (payload.roomId === selectedRoomRef.current) {
                    void fetchMessages(payload.roomId)
                        .then(setMessages)
                        .catch(console.log);
                }
            }
        );

        connection.on("RoomUpdated", () => {
            void refreshRooms().then((nextRooms) => {
                const selectedId = selectedRoomRef.current;

                if (
                    selectedId &&
                    !nextRooms.some((room) => room.id === selectedId)
                ) {
                    const nextRoomId = nextRooms[0]?.id ?? "";
                    setSelectedRoomId(nextRoomId);
                    setMessages([]);
                }
            });
        });

        connection.on("MemberAdded", () => {
            void refreshRooms();
        });

        connection.on("MemberRemoved", () => {
            void refreshRooms();
        });

        connectionRef.current = connection;

        connection
            .start()
            .then(() => setConnectionStatus("connected"))
            .catch((error) => {
                console.log(error);
                setConnectionStatus("disconnected");
                setErrorMessage(
                    "Không kết nối được realtime chat. Bạn vẫn có thể gửi bằng API thường."
                );
            });

        return () => {
            if (typingTimerRef.current) {
                window.clearTimeout(typingTimerRef.current);
            }

            connection.stop().catch(console.log);
        };
    }, [currentUserId, fetchMessages, markAsRead, refreshRooms]);

    useEffect(() => {
        selectedRoomRef.current = selectedRoomId;

        if (!selectedRoomId) {
            return;
        }

        let ignore = false;
        const connection = connectionRef.current;

        const loadRoom = async () => {
            setIsLoadingMessages(true);
            setTypingText("");
            setErrorMessage("");

            try {
                if (
                    connection &&
                    connection.state ===
                        HubConnectionState.Connected
                ) {
                    await connection.invoke("JoinRoom", selectedRoomId);
                }

                const data = await fetchMessages(selectedRoomId);

                if (ignore) {
                    return;
                }

                setMessages(data);
                await markAsRead(selectedRoomId);
                await refreshRooms();
            } catch (error) {
                console.log(error);

                if (!ignore) {
                    setErrorMessage(
                        getApiErrorMessage(
                            error,
                            "Không mở được phòng chat này."
                        )
                    );
                }
            } finally {
                if (!ignore) {
                    setIsLoadingMessages(false);
                }
            }
        };

        void loadRoom();

        return () => {
            ignore = true;

            if (
                connection &&
                connection.state === HubConnectionState.Connected
            ) {
                connection
                    .invoke("LeaveRoom", selectedRoomId)
                    .catch(console.log);
            }
        };
    }, [fetchMessages, markAsRead, refreshRooms, selectedRoomId]);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({
            behavior: "smooth",
            block: "end",
        });
    }, [messages, selectedRoomId, typingText]);

    const selectRoom = (roomId: string) => {
        const room = rooms.find((item) => item.id === roomId);

        setSelectedRoomId(roomId);
        setRoomNameDraft(room?.name ?? "");
        setIsDetailsOpen(false);
        setReplyToMessage(null);
        setEditingMessage(null);
        setAttachmentUrl("");
        setMessageText("");
        setErrorMessage("");
    };

    const selectCreatedRoom = async (room: ChatRoom) => {
        await refreshRooms();
        selectRoom(room.id);
        setIsCreateOpen(false);
    };

    const openPrivateRoom = async () => {
        if (!privateUserId) {
            return;
        }

        try {
            const res = await api.post<ChatRoom>(
                `/api/chat/private/${privateUserId}`
            );

            setPrivateUserId("");
            await selectCreatedRoom(res.data);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(
                    error,
                    "Không mở được chat cá nhân."
                )
            );
        }
    };

    const openSupportRoom = async () => {
        try {
            const res = await api.post<ChatRoom>(
                "/api/chat/support",
                null,
                {
                    params: supportAdminId
                        ? { adminId: supportAdminId }
                        : undefined,
                }
            );

            setSupportAdminId("");
            await selectCreatedRoom(res.data);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(
                    error,
                    "Không mở được phòng hỗ trợ."
                )
            );
        }
    };

    const createGroup = async () => {
        if (!groupName.trim()) {
            setErrorMessage("Vui lòng nhập tên nhóm.");
            return;
        }

        try {
            const res = await api.post<ChatRoom>("/api/chat/groups", {
                name: groupName.trim(),
                memberIds: groupMemberIds,
            });

            setGroupName("");
            setGroupMemberIds([]);
            await selectCreatedRoom(res.data);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không tạo được nhóm chat.")
            );
        }
    };

    const openProjectRoom = async () => {
        if (!projectId) {
            return;
        }

        try {
            const res = await api.post<ChatRoom>(
                `/api/chat/project/${projectId}`
            );

            setProjectId("");
            await selectCreatedRoom(res.data);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(
                    error,
                    "Không mở được chat dự án."
                )
            );
        }
    };

    const openTaskRoom = async () => {
        if (!taskId) {
            return;
        }

        try {
            const res = await api.post<ChatRoom>(
                `/api/chat/task/${taskId}`
            );

            setTaskId("");
            await selectCreatedRoom(res.data);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(
                    error,
                    "Không mở được chat công việc."
                )
            );
        }
    };

    const sendTyping = () => {
        const connection = connectionRef.current;

        if (
            selectedRoomId &&
            connection &&
            connection.state === HubConnectionState.Connected &&
            !editingMessage
        ) {
            connection.invoke("Typing", selectedRoomId).catch(console.log);
        }
    };

    const resetComposer = () => {
        setMessageText("");
        setAttachmentUrl("");
        setShowAttachmentInput(false);
        setReplyToMessage(null);
        setEditingMessage(null);
    };

    const sendMessage = async () => {
        if (!selectedRoomId) {
            return;
        }

        const content = messageText.trim();
        const nextAttachmentUrl = attachmentUrl.trim();

        if (!content && !nextAttachmentUrl) {
            return;
        }

        const request = {
            content,
            messageType: inferMessageType(content, nextAttachmentUrl),
            attachmentUrl: nextAttachmentUrl || null,
            replyToMessageId: replyToMessage?.id ?? null,
        };

        setIsSending(true);
        setErrorMessage("");

        try {
            if (editingMessage) {
                const res = await api.patch<ChatMessage>(
                    `/api/chat/messages/${editingMessage.id}`,
                    {
                        ...request,
                        replyToMessageId:
                            editingMessage.replyToMessage?.id ?? null,
                    }
                );

                setMessages((current) =>
                    current.map((message) =>
                        message.id === res.data.id ? res.data : message
                    )
                );
                resetComposer();
                await refreshRooms();
                return;
            }

            const connection = connectionRef.current;

            if (
                connection &&
                connection.state === HubConnectionState.Connected
            ) {
                await connection.invoke(
                    "SendMessage",
                    selectedRoomId,
                    request
                );
            } else {
                const res = await api.post<ChatMessage>(
                    `/api/chat/rooms/${selectedRoomId}/messages`,
                    request
                );

                setMessages((current) => [...current, res.data]);
            }

            resetComposer();
            await refreshRooms();
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không gửi được tin nhắn.")
            );
        } finally {
            setIsSending(false);
        }
    };

    const startReply = (message: ChatMessage) => {
        setReplyToMessage(message);
        setEditingMessage(null);
    };

    const startEditMessage = (message: ChatMessage) => {
        setEditingMessage(message);
        setReplyToMessage(null);
        setMessageText(message.content);
        setAttachmentUrl(message.attachmentUrl ?? "");
        setShowAttachmentInput(Boolean(message.attachmentUrl));
    };

    const deleteMessage = async (messageId: string) => {
        if (!confirm("Thu hồi tin nhắn này?")) {
            return;
        }

        try {
            const res = await api.delete<ChatMessage>(
                `/api/chat/messages/${messageId}`
            );

            setMessages((current) =>
                current.map((message) =>
                    message.id === res.data.id ? res.data : message
                )
            );
            await refreshRooms();
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không thu hồi được tin nhắn.")
            );
        }
    };

    const addMember = async () => {
        if (!selectedRoomId || !memberToAdd) {
            return;
        }

        try {
            await api.post(`/api/chat/rooms/${selectedRoomId}/members`, {
                userId: memberToAdd,
                role: "Member",
            });

            setMemberToAdd("");
            await refreshRooms();
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(
                    error,
                    "Không thêm được thành viên."
                )
            );
        }
    };

    const removeMember = async (userId: string) => {
        if (!selectedRoomId || !confirm("Xóa thành viên khỏi phòng?")) {
            return;
        }

        try {
            await api.delete(
                `/api/chat/rooms/${selectedRoomId}/members/${userId}`
            );

            await refreshRooms();
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không xóa được thành viên.")
            );
        }
    };

    const renameRoom = async () => {
        if (!selectedRoomId || !roomNameDraft.trim()) {
            return;
        }

        try {
            await api.patch(`/api/chat/rooms/${selectedRoomId}`, {
                name: roomNameDraft.trim(),
            });

            await refreshRooms();
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không đổi được tên phòng.")
            );
        }
    };

    const deleteRoom = async () => {
        if (!selectedRoomId || !confirm("Xóa phòng chat này?")) {
            return;
        }

        try {
            await api.delete(`/api/chat/rooms/${selectedRoomId}`);
            const nextRooms = await refreshRooms();
            const nextRoomId = nextRooms[0]?.id ?? "";

            setSelectedRoomId(nextRoomId);
            setMessages([]);
            setIsDetailsOpen(false);
        } catch (error) {
            console.log(error);
            setErrorMessage(
                getApiErrorMessage(error, "Không xóa được phòng chat.")
            );
        }
    };

    const toggleGroupMember = (userId: string) => {
        setGroupMemberIds((current) =>
            current.includes(userId)
                ? current.filter((id) => id !== userId)
                : [...current, userId]
        );
    };

    const connectionLabel = {
        connecting: "Đang kết nối",
        connected: "Realtime",
        reconnecting: "Đang nối lại",
        disconnected: "API thường",
    }[connectionStatus];

    const renderCreatePanel = () => (
        <div className="border-b border-slate-200 bg-slate-50 p-3">
            <div className="grid grid-cols-5 gap-1 rounded-md bg-white p-1">
                {createModes.map((mode) => (
                    <button
                        key={mode.id}
                        onClick={() => setCreateMode(mode.id)}
                        className={`h-8 rounded px-2 text-xs font-semibold transition ${
                            createMode === mode.id
                                ? "bg-[#0068ff] text-white"
                                : "text-slate-500 hover:bg-slate-100"
                        }`}
                    >
                        {mode.label}
                    </button>
                ))}
            </div>

            <div className="mt-3">
                {createMode === "private" && (
                    <div className="grid grid-cols-[1fr_auto] gap-2">
                        <select
                            value={privateUserId}
                            onChange={(event) =>
                                setPrivateUserId(event.target.value)
                            }
                            className="h-10 min-w-0 rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                        >
                            <option value="">Chọn người để chat</option>
                            {users.map((user) => (
                                <option key={user.id} value={user.id}>
                                    {user.fullName} ({user.role})
                                </option>
                            ))}
                        </select>
                        <button
                            onClick={openPrivateRoom}
                            className="h-10 rounded-md bg-[#0068ff] px-3 text-sm font-bold text-white hover:bg-[#0058d6]"
                        >
                            Mở
                        </button>
                    </div>
                )}

                {createMode === "support" && (
                    <div className="grid grid-cols-[1fr_auto] gap-2">
                        <select
                            value={supportAdminId}
                            onChange={(event) =>
                                setSupportAdminId(event.target.value)
                            }
                            className="h-10 min-w-0 rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                        >
                            <option value="">
                                Admin bất kỳ đang phụ trách
                            </option>
                            {users
                                .filter((user) => user.role === "Admin")
                                .map((user) => (
                                    <option
                                        key={user.id}
                                        value={user.id}
                                    >
                                        {user.fullName}
                                    </option>
                                ))}
                        </select>
                        <button
                            onClick={openSupportRoom}
                            className="h-10 rounded-md bg-[#0068ff] px-3 text-sm font-bold text-white hover:bg-[#0058d6]"
                        >
                            Mở
                        </button>
                    </div>
                )}

                {createMode === "group" && (
                    <div className="space-y-2">
                        <input
                            value={groupName}
                            onChange={(event) =>
                                setGroupName(event.target.value)
                            }
                            placeholder="Tên nhóm"
                            className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                        />
                        <div className="max-h-32 space-y-1 overflow-y-auto rounded-md border border-slate-200 bg-white p-2">
                            {users.length === 0 ? (
                                <p className="px-1 py-2 text-sm text-slate-400">
                                    Chưa có người dùng để thêm
                                </p>
                            ) : (
                                users.map((user) => (
                                    <label
                                        key={user.id}
                                        className="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
                                    >
                                        <input
                                            type="checkbox"
                                            checked={groupMemberIds.includes(
                                                user.id
                                            )}
                                            onChange={() =>
                                                toggleGroupMember(user.id)
                                            }
                                            className="h-4 w-4 accent-[#0068ff]"
                                        />
                                        <span className="min-w-0 flex-1 truncate">
                                            {user.fullName}
                                        </span>
                                    </label>
                                ))
                            )}
                        </div>
                        <button
                            onClick={createGroup}
                            className="h-10 w-full rounded-md bg-[#0068ff] text-sm font-bold text-white hover:bg-[#0058d6]"
                        >
                            Tạo nhóm
                        </button>
                    </div>
                )}

                {createMode === "project" && (
                    <div className="grid grid-cols-[1fr_auto] gap-2">
                        <select
                            value={projectId}
                            onChange={(event) =>
                                setProjectId(event.target.value)
                            }
                            className="h-10 min-w-0 rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                        >
                            <option value="">Chọn dự án</option>
                            {projects.map((project) => (
                                <option
                                    key={project.id}
                                    value={project.id}
                                >
                                    {project.name}
                                </option>
                            ))}
                        </select>
                        <button
                            onClick={openProjectRoom}
                            className="h-10 rounded-md bg-[#0068ff] px-3 text-sm font-bold text-white hover:bg-[#0058d6]"
                        >
                            Mở
                        </button>
                    </div>
                )}

                {createMode === "task" && (
                    <div className="grid grid-cols-[1fr_auto] gap-2">
                        <select
                            value={taskId}
                            onChange={(event) =>
                                setTaskId(event.target.value)
                            }
                            className="h-10 min-w-0 rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                        >
                            <option value="">Chọn công việc</option>
                            {tasks.map((task) => (
                                <option key={task.id} value={task.id}>
                                    {task.title}
                                </option>
                            ))}
                        </select>
                        <button
                            onClick={openTaskRoom}
                            className="h-10 rounded-md bg-[#0068ff] px-3 text-sm font-bold text-white hover:bg-[#0058d6]"
                        >
                            Mở
                        </button>
                    </div>
                )}
            </div>
        </div>
    );

    const renderDetailsPanel = () => {
        if (!selectedRoom) {
            return (
                <div className="flex h-full items-center justify-center p-6 text-center text-sm text-slate-400">
                    Chọn một hội thoại để xem thông tin
                </div>
            );
        }

        return (
            <div className="flex h-full min-h-0 flex-col bg-white">
                <div className="border-b border-slate-200 p-4">
                    <div className="flex items-start justify-between gap-3">
                        <div>
                            <p className="text-sm font-bold text-slate-800">
                                Thông tin
                            </p>
                            <p className="mt-1 text-xs text-slate-500">
                                Quản lý phòng và thành viên
                            </p>
                        </div>
                        <button
                            onClick={() => setIsDetailsOpen(false)}
                            className="h-8 w-8 rounded-full text-sm font-bold text-slate-500 hover:bg-slate-100 2xl:hidden"
                            aria-label="Đóng thông tin"
                            title="Đóng"
                        >
                            x
                        </button>
                    </div>
                </div>

                <div className="min-h-0 flex-1 overflow-y-auto p-4">
                    <div className="text-center">
                        <div
                            className={`mx-auto flex h-16 w-16 items-center justify-center rounded-full text-lg font-bold text-white ${getRoomAvatarClass(
                                selectedRoom.type
                            )}`}
                        >
                            {getInitials(getRoomTitle(selectedRoom))}
                        </div>
                        <h3 className="mt-3 truncate text-base font-bold text-slate-900">
                            {getRoomTitle(selectedRoom)}
                        </h3>
                        <p className="mt-1 text-sm text-slate-500">
                            {getRoomSubtitle(selectedRoom)}
                        </p>
                    </div>

                    <div className="mt-5 space-y-2">
                        <label className="text-xs font-bold uppercase text-slate-400">
                            Tên phòng
                        </label>
                        <div className="grid grid-cols-[1fr_auto] gap-2">
                            <input
                                value={roomNameDraft}
                                onChange={(event) =>
                                    setRoomNameDraft(event.target.value)
                                }
                                placeholder="Đổi tên phòng"
                                className="h-10 min-w-0 rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-[#0068ff]"
                            />
                            <button
                                onClick={renameRoom}
                                className="h-10 rounded-md border border-slate-200 px-3 text-sm font-bold text-slate-700 hover:bg-slate-50"
                            >
                                Lưu
                            </button>
                        </div>
                    </div>

                    <div className="mt-5 space-y-2">
                        <label className="text-xs font-bold uppercase text-slate-400">
                            Thêm thành viên
                        </label>
                        <div className="grid grid-cols-[1fr_auto] gap-2">
                            <select
                                value={memberToAdd}
                                onChange={(event) =>
                                    setMemberToAdd(event.target.value)
                                }
                                className="h-10 min-w-0 rounded-md border border-slate-200 bg-white px-3 text-sm outline-none focus:border-[#0068ff]"
                            >
                                <option value="">Chọn thành viên</option>
                                {addableUsers.map((user) => (
                                    <option
                                        key={user.id}
                                        value={user.id}
                                    >
                                        {user.fullName}
                                    </option>
                                ))}
                            </select>
                            <button
                                onClick={addMember}
                                className="h-10 rounded-md bg-[#0068ff] px-3 text-sm font-bold text-white hover:bg-[#0058d6]"
                            >
                                Thêm
                            </button>
                        </div>
                    </div>

                    <div className="mt-5">
                        <div className="mb-2 flex items-center justify-between">
                            <p className="text-xs font-bold uppercase text-slate-400">
                                Thành viên
                            </p>
                            <span className="text-xs font-semibold text-slate-500">
                                {selectedRoom.members.length}
                            </span>
                        </div>

                        <div className="space-y-2">
                            {selectedRoom.members.map((member) => {
                                const isMe = sameId(
                                    member.userId,
                                    currentUserId
                                );

                                return (
                                    <div
                                        key={member.userId}
                                        className="flex items-center gap-3 rounded-md border border-slate-100 p-2"
                                    >
                                        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-200 text-xs font-bold text-slate-700">
                                            {getInitials(member.fullName)}
                                        </div>
                                        <div className="min-w-0 flex-1">
                                            <p className="truncate text-sm font-bold text-slate-800">
                                                {member.fullName}
                                                {isMe ? " (Bạn)" : ""}
                                            </p>
                                            <p className="text-xs text-slate-500">
                                                {
                                                    memberRoleLabels[
                                                        member.role
                                                    ]
                                                }
                                            </p>
                                        </div>
                                        {!isMe && (
                                            <button
                                                onClick={() =>
                                                    removeMember(
                                                        member.userId
                                                    )
                                                }
                                                className="h-8 w-8 rounded-full text-sm font-bold text-red-500 hover:bg-red-50"
                                                aria-label={`Xóa ${member.fullName}`}
                                                title="Xóa khỏi phòng"
                                            >
                                                x
                                            </button>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                </div>

                <div className="border-t border-slate-200 p-4">
                    <button
                        onClick={deleteRoom}
                        className="h-10 w-full rounded-md border border-red-200 bg-red-50 text-sm font-bold text-red-700 hover:bg-red-100"
                    >
                        Xóa phòng chat
                    </button>
                </div>
            </div>
        );
    };

    return (
        <div className="h-[calc(100vh-2.5rem)] min-h-[680px] overflow-hidden rounded-md border border-slate-200 bg-white text-slate-800 shadow-sm">
            <div className="grid h-full grid-cols-[320px_minmax(0,1fr)] 2xl:grid-cols-[320px_minmax(0,1fr)_300px]">
                <aside className="flex min-h-0 flex-col border-r border-slate-200 bg-white">
                    <div className="border-b border-slate-200 p-4">
                        <div className="flex items-center justify-between gap-3">
                            <div>
                                <h1 className="text-xl font-bold text-slate-900">
                                    Tin nhắn
                                </h1>
                                <p className="mt-1 text-xs font-semibold text-slate-500">
                                    {connectionLabel}
                                </p>
                            </div>
                            <button
                                onClick={() =>
                                    setIsCreateOpen((current) => !current)
                                }
                                className="h-10 w-10 rounded-full bg-[#0068ff] text-xl font-bold leading-none text-white hover:bg-[#0058d6]"
                                aria-label="Tạo hội thoại"
                                title="Tạo hội thoại"
                            >
                                +
                            </button>
                        </div>

                        <div className="mt-4">
                            <input
                                value={searchText}
                                onChange={(event) =>
                                    setSearchText(event.target.value)
                                }
                                placeholder="Tìm kiếm"
                                className="h-10 w-full rounded-md border border-slate-200 bg-slate-50 px-3 text-sm outline-none transition focus:border-[#0068ff] focus:bg-white"
                            />
                        </div>
                    </div>

                    {isCreateOpen && renderCreatePanel()}

                    <div className="min-h-0 flex-1 overflow-y-auto">
                        {isLoading ? (
                            <div className="space-y-2 p-3">
                                {Array.from({ length: 6 }).map(
                                    (_, index) => (
                                        <div
                                            key={index}
                                            className="flex animate-pulse items-center gap-3 rounded-md p-2"
                                        >
                                            <div className="h-11 w-11 rounded-full bg-slate-200" />
                                            <div className="min-w-0 flex-1 space-y-2">
                                                <div className="h-3 w-2/3 rounded bg-slate-200" />
                                                <div className="h-3 w-full rounded bg-slate-100" />
                                            </div>
                                        </div>
                                    )
                                )}
                            </div>
                        ) : filteredRooms.length === 0 ? (
                            <div className="p-6 text-center text-sm text-slate-400">
                                Không có hội thoại phù hợp
                            </div>
                        ) : (
                            filteredRooms.map((room) => {
                                const selected = room.id === selectedRoomId;

                                return (
                                    <button
                                        key={room.id}
                                        onClick={() => selectRoom(room.id)}
                                        className={`w-full border-b border-slate-100 px-3 py-3 text-left transition ${
                                            selected
                                                ? "bg-[#eaf2ff]"
                                                : "bg-white hover:bg-slate-50"
                                        }`}
                                    >
                                        <div className="flex gap-3">
                                            <div
                                                className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white ${getRoomAvatarClass(
                                                    room.type
                                                )}`}
                                            >
                                                {getInitials(
                                                    getRoomTitle(room)
                                                )}
                                            </div>
                                            <div className="min-w-0 flex-1">
                                                <div className="flex items-center justify-between gap-2">
                                                    <p className="truncate text-sm font-bold text-slate-900">
                                                        {getRoomTitle(room)}
                                                    </p>
                                                    <span className="shrink-0 text-[11px] font-semibold text-slate-400">
                                                        {formatConversationTime(
                                                            room
                                                                .lastMessage
                                                                ?.createdAt ??
                                                                room.createdAt
                                                        )}
                                                    </span>
                                                </div>
                                                <div className="mt-1 flex items-center gap-2">
                                                    <p
                                                        className={`min-w-0 flex-1 truncate text-sm ${
                                                            room.unreadCount >
                                                            0
                                                                ? "font-bold text-slate-900"
                                                                : "text-slate-500"
                                                        }`}
                                                    >
                                                        {getLastMessagePreview(
                                                            room
                                                        )}
                                                    </p>
                                                    {room.unreadCount > 0 && (
                                                        <span className="inline-flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-red-500 px-1.5 text-[11px] font-bold text-white">
                                                            {
                                                                room.unreadCount
                                                            }
                                                        </span>
                                                    )}
                                                </div>
                                                <p className="mt-1 truncate text-xs text-slate-400">
                                                    {getRoomSubtitle(room)}
                                                </p>
                                            </div>
                                        </div>
                                    </button>
                                );
                            })
                        )}
                    </div>
                </aside>

                <section className="flex min-h-0 flex-col bg-[#eef3f8]">
                    {selectedRoom ? (
                        <>
                            <div className="flex h-16 shrink-0 items-center justify-between gap-3 border-b border-slate-200 bg-white px-4">
                                <div className="flex min-w-0 items-center gap-3">
                                    <div
                                        className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white ${getRoomAvatarClass(
                                            selectedRoom.type
                                        )}`}
                                    >
                                        {getInitials(
                                            getRoomTitle(selectedRoom)
                                        )}
                                    </div>
                                    <div className="min-w-0">
                                        <h2 className="truncate text-base font-bold text-slate-900">
                                            {getRoomTitle(selectedRoom)}
                                        </h2>
                                        <p className="truncate text-xs font-semibold text-slate-500">
                                            {onlineMembers.length > 0
                                                ? `${onlineMembers.length} đang hoạt động`
                                                : getRoomSubtitle(
                                                      selectedRoom
                                                  )}
                                        </p>
                                    </div>
                                </div>

                                <button
                                    onClick={() => setIsDetailsOpen(true)}
                                    className="h-10 w-10 rounded-full text-sm font-bold text-[#0068ff] hover:bg-[#eaf2ff] 2xl:hidden"
                                    aria-label="Thông tin hội thoại"
                                    title="Thông tin"
                                >
                                    i
                                </button>
                            </div>

                            {errorMessage && (
                                <div className="border-b border-amber-200 bg-amber-50 px-4 py-2 text-sm font-semibold text-amber-800">
                                    {errorMessage}
                                </div>
                            )}

                            <div className="min-h-0 flex-1 overflow-y-auto px-4 py-5">
                                {isLoadingMessages ? (
                                    <div className="space-y-4">
                                        {Array.from({ length: 5 }).map(
                                            (_, index) => (
                                                <div
                                                    key={index}
                                                    className={`flex animate-pulse ${
                                                        index % 2 === 0
                                                            ? "justify-start"
                                                            : "justify-end"
                                                    }`}
                                                >
                                                    <div className="h-16 w-64 rounded-md bg-white/80" />
                                                </div>
                                            )
                                        )}
                                    </div>
                                ) : messages.length === 0 ? (
                                    <div className="flex h-full items-center justify-center text-center">
                                        <div>
                                            <div
                                                className={`mx-auto flex h-16 w-16 items-center justify-center rounded-full text-lg font-bold text-white ${getRoomAvatarClass(
                                                    selectedRoom.type
                                                )}`}
                                            >
                                                {getInitials(
                                                    getRoomTitle(
                                                        selectedRoom
                                                    )
                                                )}
                                            </div>
                                            <p className="mt-4 text-sm font-semibold text-slate-500">
                                                Bắt đầu cuộc trò chuyện
                                            </p>
                                        </div>
                                    </div>
                                ) : (
                                    <div className="space-y-3">
                                        {messages.map((message, index) => {
                                            const isMine = sameId(
                                                message.senderId,
                                                currentUserId
                                            );
                                            const showDay =
                                                index === 0 ||
                                                !isSameDay(
                                                    messages[index - 1]
                                                        .createdAt,
                                                    message.createdAt
                                                );
                                            const canDelete =
                                                isMine || isCurrentUserAdmin;
                                            const seenByOthers =
                                                message.readBy.filter(
                                                    (read) =>
                                                        !sameId(
                                                            read.userId,
                                                            currentUserId
                                                        )
                                                ).length;

                                            return (
                                                <div key={message.id}>
                                                    {showDay && (
                                                        <div className="my-4 flex justify-center">
                                                            <span className="rounded-full bg-white/80 px-3 py-1 text-xs font-semibold text-slate-500 shadow-sm">
                                                                {formatMessageDay(
                                                                    message.createdAt
                                                                )}
                                                            </span>
                                                        </div>
                                                    )}

                                                    <div
                                                        className={`flex gap-2 ${
                                                            isMine
                                                                ? "justify-end"
                                                                : "justify-start"
                                                        }`}
                                                    >
                                                        {!isMine && (
                                                            <div className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-white text-xs font-bold text-slate-600 shadow-sm">
                                                                {getInitials(
                                                                    message.senderName
                                                                )}
                                                            </div>
                                                        )}

                                                        <div
                                                            className={`group max-w-[72%] ${
                                                                isMine
                                                                    ? "items-end"
                                                                    : "items-start"
                                                            }`}
                                                        >
                                                            {!isMine &&
                                                                selectedRoom
                                                                    .members
                                                                    .length >
                                                                    2 && (
                                                                    <p className="mb-1 px-1 text-xs font-semibold text-slate-500">
                                                                        {
                                                                            message.senderName
                                                                        }
                                                                    </p>
                                                                )}

                                                            <div
                                                                className={`rounded-md px-3 py-2 shadow-sm ${
                                                                    isMine
                                                                        ? "bg-[#0068ff] text-white"
                                                                        : "bg-white text-slate-900"
                                                                } ${
                                                                    message.isDeleted
                                                                        ? "opacity-70"
                                                                        : ""
                                                                }`}
                                                            >
                                                                {message.replyToMessage && (
                                                                    <div
                                                                        className={`mb-2 border-l-2 px-2 py-1 text-xs ${
                                                                            isMine
                                                                                ? "border-white/70 bg-white/15"
                                                                                : "border-[#0068ff] bg-slate-50 text-slate-600"
                                                                        }`}
                                                                    >
                                                                        <p className="font-bold">
                                                                            {
                                                                                message
                                                                                    .replyToMessage
                                                                                    .senderName
                                                                            }
                                                                        </p>
                                                                        <p className="line-clamp-2">
                                                                            {message
                                                                                .replyToMessage
                                                                                .isDeleted
                                                                                ? "Tin nhắn đã thu hồi"
                                                                                : message
                                                                                      .replyToMessage
                                                                                      .content ||
                                                                                  "Tệp đính kèm"}
                                                                        </p>
                                                                    </div>
                                                                )}

                                                                <p className="whitespace-pre-wrap break-words text-sm leading-6">
                                                                    {message.isDeleted
                                                                        ? "Tin nhắn đã thu hồi"
                                                                        : message.content}
                                                                </p>

                                                                {!message.isDeleted &&
                                                                    message.attachmentUrl && (
                                                                        <a
                                                                            href={
                                                                                message.attachmentUrl
                                                                            }
                                                                            target="_blank"
                                                                            rel="noreferrer"
                                                                            className={`mt-2 block truncate rounded border px-2 py-1 text-xs font-semibold underline ${
                                                                                isMine
                                                                                    ? "border-white/30 bg-white/15 text-white"
                                                                                    : "border-slate-200 bg-slate-50 text-[#0068ff]"
                                                                            }`}
                                                                        >
                                                                            {message.messageType ===
                                                                            "Image"
                                                                                ? "Mở ảnh đính kèm"
                                                                                : "Mở tệp đính kèm"}
                                                                        </a>
                                                                    )}

                                                                <div
                                                                    className={`mt-1 flex items-center justify-end gap-1 text-[11px] ${
                                                                        isMine
                                                                            ? "text-white/75"
                                                                            : "text-slate-400"
                                                                    }`}
                                                                >
                                                                    <span>
                                                                        {formatMessageTime(
                                                                            message.createdAt
                                                                        )}
                                                                    </span>
                                                                    {message.editedAt &&
                                                                        !message.isDeleted && (
                                                                            <span>
                                                                                Đã sửa
                                                                            </span>
                                                                        )}
                                                                </div>
                                                            </div>

                                                            {!message.isDeleted && (
                                                                <div
                                                                    className={`mt-1 flex gap-2 px-1 text-xs font-semibold text-slate-500 opacity-0 transition group-hover:opacity-100 ${
                                                                        isMine
                                                                            ? "justify-end"
                                                                            : "justify-start"
                                                                    }`}
                                                                >
                                                                    <button
                                                                        onClick={() =>
                                                                            startReply(
                                                                                message
                                                                            )
                                                                        }
                                                                        className="hover:text-[#0068ff]"
                                                                    >
                                                                        Trả lời
                                                                    </button>
                                                                    {isMine && (
                                                                        <button
                                                                            onClick={() =>
                                                                                startEditMessage(
                                                                                    message
                                                                                )
                                                                            }
                                                                            className="hover:text-[#0068ff]"
                                                                        >
                                                                            Sửa
                                                                        </button>
                                                                    )}
                                                                    {canDelete && (
                                                                        <button
                                                                            onClick={() =>
                                                                                deleteMessage(
                                                                                    message.id
                                                                                )
                                                                            }
                                                                            className="hover:text-red-600"
                                                                        >
                                                                            Thu hồi
                                                                        </button>
                                                                    )}
                                                                </div>
                                                            )}

                                                            {isMine &&
                                                                !message.isDeleted && (
                                                                    <p className="mt-1 px-1 text-right text-[11px] font-semibold text-slate-400">
                                                                        {seenByOthers >
                                                                        0
                                                                            ? `Đã xem bởi ${seenByOthers}`
                                                                            : "Đã gửi"}
                                                                    </p>
                                                                )}
                                                        </div>
                                                    </div>
                                                </div>
                                            );
                                        })}
                                        <div ref={messagesEndRef} />
                                    </div>
                                )}
                            </div>

                            <div className="shrink-0 border-t border-slate-200 bg-white p-3">
                                <div className="h-5 px-1 text-sm font-semibold text-[#0068ff]">
                                    {typingText}
                                </div>

                                {(replyToMessage || editingMessage) && (
                                    <div className="mb-2 flex items-start gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2">
                                        <div className="min-w-0 flex-1">
                                            <p className="text-xs font-bold text-slate-600">
                                                {editingMessage
                                                    ? "Đang sửa tin nhắn"
                                                    : `Trả lời ${replyToMessage?.senderName}`}
                                            </p>
                                            <p className="mt-1 line-clamp-2 text-sm text-slate-500">
                                                {editingMessage
                                                    ? editingMessage.content ||
                                                      "Tệp đính kèm"
                                                    : replyToMessage?.content ||
                                                      "Tệp đính kèm"}
                                            </p>
                                        </div>
                                        <button
                                            onClick={resetComposer}
                                            className="h-7 w-7 rounded-full text-sm font-bold text-slate-500 hover:bg-slate-200"
                                            aria-label="Hủy thao tác"
                                            title="Hủy"
                                        >
                                            x
                                        </button>
                                    </div>
                                )}

                                {showAttachmentInput && (
                                    <input
                                        value={attachmentUrl}
                                        onChange={(event) =>
                                            setAttachmentUrl(
                                                event.target.value
                                            )
                                        }
                                        placeholder="Dán link ảnh hoặc tệp đính kèm"
                                        className="mb-2 h-10 w-full rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-[#0068ff]"
                                    />
                                )}

                                <div className="grid grid-cols-[auto_1fr_auto] items-end gap-2">
                                    <button
                                        onClick={() =>
                                            setShowAttachmentInput(
                                                (current) => !current
                                            )
                                        }
                                        className={`h-10 w-10 rounded-full text-lg font-bold ${
                                            showAttachmentInput
                                                ? "bg-[#eaf2ff] text-[#0068ff]"
                                                : "text-slate-500 hover:bg-slate-100"
                                        }`}
                                        aria-label="Đính kèm link"
                                        title="Đính kèm link"
                                    >
                                        +
                                    </button>
                                    <textarea
                                        rows={1}
                                        value={messageText}
                                        onChange={(event) => {
                                            setMessageText(
                                                event.target.value
                                            );
                                            sendTyping();
                                        }}
                                        onKeyDown={(event) => {
                                            if (
                                                event.key === "Enter" &&
                                                !event.shiftKey
                                            ) {
                                                event.preventDefault();
                                                void sendMessage();
                                            }
                                        }}
                                        placeholder={
                                            editingMessage
                                                ? "Nhập nội dung mới..."
                                                : "Nhập tin nhắn..."
                                        }
                                        className="max-h-32 min-h-10 resize-none rounded-md border border-slate-200 px-3 py-2 text-sm leading-6 outline-none focus:border-[#0068ff]"
                                    />
                                    <button
                                        onClick={sendMessage}
                                        disabled={isSending}
                                        className="h-10 rounded-md bg-[#0068ff] px-5 text-sm font-bold text-white hover:bg-[#0058d6] disabled:cursor-not-allowed disabled:opacity-60"
                                    >
                                        {isSending
                                            ? "Đang gửi"
                                            : editingMessage
                                              ? "Lưu"
                                              : "Gửi"}
                                    </button>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="flex h-full items-center justify-center text-center">
                            <div>
                                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-[#0068ff] text-lg font-bold text-white">
                                    +
                                </div>
                                <p className="mt-4 text-sm font-semibold text-slate-500">
                                    Chọn hoặc tạo một hội thoại
                                </p>
                            </div>
                        </div>
                    )}
                </section>

                <aside className="hidden min-h-0 border-l border-slate-200 2xl:flex 2xl:flex-col">
                    {renderDetailsPanel()}
                </aside>
            </div>

            {isDetailsOpen && (
                <div className="fixed inset-0 z-50 bg-black/30 2xl:hidden">
                    <div className="ml-auto h-full w-full max-w-[360px] shadow-xl">
                        {renderDetailsPanel()}
                    </div>
                </div>
            )}
        </div>
    );
}
