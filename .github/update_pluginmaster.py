import json, os, sys, time

repo_path = sys.argv[1]
version = sys.argv[2]
repo_full_name = sys.argv[3]

json_path = os.path.join(repo_path, 'pluginmaster.json')

with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

download_url = f"https://github.com/{repo_full_name}/releases/download/v{version}/ExtraChat.zip"

entry = {
    'Name': 'ExtraChat',
    'Author': 'Anna, QianChangUwU',
    'Punchline': '跨数据中心、不限成员数量的加密聊天频道。',
    'Description': 'ExtraChat 添加端到端加密、跨数据中心、不限成员数量的额外聊天频道到游戏中。',
    'InternalName': 'ExtraChat',
    'AssemblyVersion': version,
    'TestingAssemblyVersion': version,
    'DalamudApiLevel': 15,
    'TestingDalamudApiLevel': 15,
    'DownloadLinkInstall': download_url,
    'DownloadLinkUpdate': download_url,
    'DownloadLinkTesting': download_url,
    'RepoUrl': f'https://github.com/{repo_full_name}',
    'IconUrl': download_url,
    'Tags': ['chat', 'linkshell', 'cn'],
    'ApplicableVersion': 'any',
    'LoadPriority': 0,
    'AcceptsFeedback': True,
    'LastUpdate': int(time.time()),
}

idx = next((i for i, e in enumerate(data) if e.get('InternalName') == 'ExtraChat'), -1)
if idx >= 0:
    data[idx] = entry
else:
    data.append(entry)

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=4, ensure_ascii=False)
