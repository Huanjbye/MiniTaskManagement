"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { api } from "@/lib/api";

type ProjectOption = {
    id: string;
    name: string;
};

type TaskResponse = {
    title?: string;
    description?: string | null;
    status?: string;
    priority?: string;
    dueDate?: string;
    reminderAt?: string | null;
    estimatedMinutes?: number | null;
    actualMinutes?: number;
    projectId?: string | null;
    tagNames?: string[];
};

const toDateInput = (value?: string | null) =>
    value ? value.split("T")[0] : "";

const toDateTimeInput = (value?: string | null) =>
    value ? value.slice(0, 16) : "";

const toNumberOrNull = (value: string) => {
    if (!value.trim()) {
        return null;
    }

    return Number(value);
};

const splitTags = (value: string) =>
    value
        .split(",")
        .map((tag) => tag.trim())
        .filter(Boolean);

export default function EditTask() {
    const router = useRouter();
    const params = useParams();
    const id = Array.isArray(params.id)
        ? params.id[0]
        : params.id;

    const [projects, setProjects] = useState<ProjectOption[]>([]);

    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [status, setStatus] = useState("Todo");
    const [priority, setPriority] = useState("Medium");
    const [projectId, setProjectId] = useState("");
    const [dueDate, setDueDate] = useState("");
    const [reminderAt, setReminderAt] = useState("");
    const [estimatedMinutes, setEstimatedMinutes] =
        useState("");
    const [actualMinutes, setActualMinutes] = useState("0");
    const [tagInput, setTagInput] = useState("");
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadProjects = async () => {
            try {
                const res = await api.get<ProjectOption[]>(
                    "/api/projects"
                );

                setProjects(res.data);
            } catch (err) {
                console.log(err);
            }
        };

        void loadProjects();
    }, []);

    useEffect(() => {
        if (!id) {
            return;
        }

        const fetchTask = async () => {
            try {
                const res = await api.get<TaskResponse>(
                    `/api/tasks/${id}`
                );

                setTitle(res.data.title ?? "");
                setDescription(res.data.description ?? "");
                setStatus(res.data.status ?? "Todo");
                setPriority(res.data.priority ?? "Medium");
                setProjectId(res.data.projectId ?? "");
                setDueDate(toDateInput(res.data.dueDate));
                setReminderAt(
                    toDateTimeInput(res.data.reminderAt)
                );
                setEstimatedMinutes(
                    res.data.estimatedMinutes?.toString() ?? ""
                );
                setActualMinutes(
                    (res.data.actualMinutes ?? 0).toString()
                );
                setTagInput(
                    (res.data.tagNames ?? []).join(", ")
                );
            } catch (err) {
                console.log(err);
            } finally {
                setLoading(false);
            }
        };

        void fetchTask();
    }, [id]);

    const updateTask = async () => {
        try {
            await api.put(`/api/tasks/${id}`, {
                title,
                description,
                status,
                priority,
                projectId: projectId || null,
                dueDate,
                reminderAt: reminderAt || null,
                estimatedMinutes:
                    toNumberOrNull(estimatedMinutes),
                actualMinutes:
                    toNumberOrNull(actualMinutes) ?? 0,
                tagNames: splitTags(tagInput),
            });

            router.push("/dashboard");
        } catch (err) {
            console.log(err);
            alert("Cập nhật công việc thất bại");
        }
    };

    if (loading) {
        return (
            <div className="flex min-h-[420px] items-center justify-center">
                <div className="rounded-md border bg-white px-8 py-5 shadow-sm">
                    Đang tải...
                </div>
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-5xl">
            <div className="rounded-md border border-slate-200 bg-white p-6 shadow-sm">
                <div className="mb-6">
                    <h1 className="text-2xl font-bold text-slate-800">
                        Sửa công việc
                    </h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Cập nhật thông tin, trạng thái, tag và thời gian
                        xử lý.
                    </p>
                </div>

                <div className="space-y-5">
                    <div>
                        <label className="mb-2 block text-sm font-semibold">
                            Tiêu đề
                        </label>
                        <input
                            type="text"
                            value={title}
                            onChange={(event) =>
                                setTitle(event.target.value)
                            }
                            className="w-full rounded-md border px-4 py-3 outline-none focus:border-[#16b99f]"
                            placeholder="Tiêu đề công việc"
                        />
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-semibold">
                            Mô tả
                        </label>
                        <textarea
                            rows={5}
                            value={description}
                            onChange={(event) =>
                                setDescription(event.target.value)
                            }
                            className="w-full resize-none rounded-md border px-4 py-3 outline-none focus:border-[#16b99f]"
                            placeholder="Mô tả công việc"
                        />
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Project
                            </label>
                            <select
                                value={projectId}
                                onChange={(event) =>
                                    setProjectId(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border px-4 py-3"
                            >
                                <option value="">
                                    Không gắn project
                                </option>
                                {projects.map((project) => (
                                    <option
                                        key={project.id}
                                        value={project.id}
                                    >
                                        {project.name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Trạng thái
                            </label>
                            <select
                                value={status}
                                onChange={(event) =>
                                    setStatus(event.target.value)
                                }
                                className="w-full rounded-md border px-4 py-3"
                            >
                                <option value="Todo">
                                    Chưa làm
                                </option>
                                <option value="InProgress">
                                    Đang làm
                                </option>
                                <option value="Review">
                                    Đang review
                                </option>
                                <option value="Done">
                                    Hoàn thành
                                </option>
                            </select>
                        </div>
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Độ ưu tiên
                            </label>
                            <select
                                value={priority}
                                onChange={(event) =>
                                    setPriority(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border px-4 py-3"
                            >
                                <option value="Low">Thấp</option>
                                <option value="Medium">
                                    Trung bình
                                </option>
                                <option value="High">Cao</option>
                                <option value="Critical">
                                    Khẩn cấp
                                </option>
                            </select>
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Hạn xử lý
                            </label>
                            <input
                                type="date"
                                value={dueDate}
                                min={
                                    new Date()
                                        .toISOString()
                                        .split("T")[0]
                                }
                                onChange={(event) =>
                                    setDueDate(event.target.value)
                                }
                                className="w-full rounded-md border px-4 py-3"
                            />
                        </div>
                    </div>

                    <div className="grid gap-5 md:grid-cols-3">
                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Nhắc trước hạn
                            </label>
                            <input
                                type="datetime-local"
                                value={reminderAt}
                                onChange={(event) =>
                                    setReminderAt(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border px-4 py-3"
                            />
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Ước tính (phút)
                            </label>
                            <input
                                type="number"
                                min="0"
                                value={estimatedMinutes}
                                onChange={(event) =>
                                    setEstimatedMinutes(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border px-4 py-3"
                            />
                        </div>

                        <div>
                            <label className="mb-2 block text-sm font-semibold">
                                Thực tế (phút)
                            </label>
                            <input
                                type="number"
                                min="0"
                                value={actualMinutes}
                                onChange={(event) =>
                                    setActualMinutes(
                                        event.target.value
                                    )
                                }
                                className="w-full rounded-md border px-4 py-3"
                            />
                        </div>
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-semibold">
                            Tag
                        </label>
                        <input
                            type="text"
                            value={tagInput}
                            placeholder="Ví dụ: Backend, Bug, Feature"
                            onChange={(event) =>
                                setTagInput(event.target.value)
                            }
                            className="w-full rounded-md border px-4 py-3 outline-none focus:border-[#16b99f]"
                        />
                    </div>

                    <div className="flex justify-end gap-3 pt-3">
                        <button
                            onClick={() => router.push("/dashboard")}
                            className="rounded-md border px-5 py-3 font-semibold hover:bg-slate-50"
                        >
                            Hủy
                        </button>
                        <button
                            onClick={updateTask}
                            className="rounded-md bg-[#16b99f] px-5 py-3 font-bold text-white hover:bg-[#119f89]"
                        >
                            Cập nhật
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
