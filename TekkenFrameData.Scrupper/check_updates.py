import subprocess
import sys

# ��������� ���������� ������
result = subprocess.run(["pip", "list", "--outdated"], capture_output=True, text=True)

# ���� ���� ���������� ������, ��������� � �������
if result.stdout:
    print("���������� ���������� ������:")
    print(result.stdout)
    sys.exit(1)
else:
    print("��� ������ ���������.")